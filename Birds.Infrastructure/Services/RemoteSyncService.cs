using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Birds.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace Birds.Infrastructure.Services
{
    public sealed class RemoteSyncService(
        IDbContextFactory<BirdDbContext> localContextFactory,
        IDbContextFactory<RemoteBirdDbContext> remoteContextFactory,
        ILogger<RemoteSyncService> logger) : IRemoteSyncService
    {
        private readonly record struct PushBatchResult(int ProcessedCount, int RemoteWinsCount);

        private const int PushBatchSize = 128;
        private const int PullBatchSize = 128;
        private const string BirdsPullCursorKey = "Birds.Pull";
        private const string BirdDeletesPullCursorKey = "Birds.Deletes.Pull";

        private readonly IDbContextFactory<BirdDbContext> _localContextFactory = localContextFactory;
        private readonly IDbContextFactory<RemoteBirdDbContext> _remoteContextFactory = remoteContextFactory;
        private readonly ILogger<RemoteSyncService> _logger = logger;
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        public async Task<RemoteSyncRunResult> SyncPendingAsync(CancellationToken cancellationToken)
        {
            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                await using var localContext = await _localContextFactory.CreateDbContextAsync(cancellationToken);
                var pendingOperations = await localContext.SyncOperations
                    .OrderBy(operation => operation.CreatedAtUtc)
                    .Take(PushBatchSize)
                    .ToListAsync(cancellationToken);

                try
                {
                    await using var remoteContext = await _remoteContextFactory.CreateDbContextAsync(cancellationToken);
                    if (!await remoteContext.Database.CanConnectAsync(cancellationToken))
                    {
                        const string backendUnavailableMessage = "Remote sync backend is unavailable.";
                        return await HandleRemoteFailureAsync(
                            localContext,
                            pendingOperations,
                            RemoteSyncRunStatus.BackendUnavailable,
                            backendUnavailableMessage,
                            null,
                            cancellationToken);
                    }

                    var processedCount = 0;
                    var remoteWinsCount = 0;
                    await EnsureRemoteSyncSchemaAsync(remoteContext, cancellationToken);

                    if (pendingOperations.Count > 0)
                    {
                        var pushResult = await PushPendingBatchAsync(localContext, remoteContext, pendingOperations, cancellationToken);
                        processedCount += pushResult.ProcessedCount;
                        remoteWinsCount += pushResult.RemoteWinsCount;
                    }

                    var hasPendingLocalOperations = await localContext.SyncOperations.AnyAsync(cancellationToken);
                    if (!hasPendingLocalOperations)
                    {
                        processedCount += await PullRemoteBatchAsync(localContext, remoteContext, cancellationToken);
                        processedCount += await PullRemoteDeletesAsync(localContext, remoteContext, cancellationToken);
                    }

                    return processedCount > 0
                        ? new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, processedCount, null, remoteWinsCount)
                        : RemoteSyncRunResult.NothingToSync;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var status = IsBackendUnavailable(ex)
                        ? RemoteSyncRunStatus.BackendUnavailable
                        : RemoteSyncRunStatus.Failed;

                    return await HandleRemoteFailureAsync(
                        localContext,
                        pendingOperations,
                        status,
                        ex.Message,
                        ex,
                        cancellationToken);
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task<PushBatchResult> PushPendingBatchAsync(BirdDbContext localContext,
                                                                  RemoteBirdDbContext remoteContext,
                                                                  IReadOnlyCollection<SyncOperation> pendingOperations,
                                                                  CancellationToken cancellationToken)
        {
            var remoteWinsCount = await ApplyPushBatchAsync(localContext, remoteContext, pendingOperations, cancellationToken);
            localContext.SyncOperations.RemoveRange(pendingOperations);
            await localContext.SaveChangesAsync(cancellationToken);
            localContext.ChangeTracker.Clear();

            _logger.LogInformation(LogMessages.RemoteSyncProcessed, pendingOperations.Count);
            return new PushBatchResult(pendingOperations.Count, remoteWinsCount);
        }

        private async Task<int> PullRemoteBatchAsync(BirdDbContext localContext,
                                                     RemoteBirdDbContext remoteContext,
                                                     CancellationToken cancellationToken)
        {
            var cursor = await localContext.RemoteSyncCursors
                .SingleOrDefaultAsync(syncCursor => syncCursor.CursorKey == BirdsPullCursorKey, cancellationToken);
            var watermarkUtc = cursor?.LastSyncedAtUtc;

            var remoteChanges = await remoteContext.Birds
                .AsNoTracking()
                .Select(bird => new
                {
                    Bird = bird,
                    SyncStamp = bird.UpdatedAt ?? bird.CreatedAt
                })
                .Where(change => watermarkUtc == null || change.SyncStamp > watermarkUtc.Value)
                .OrderBy(change => change.SyncStamp)
                .ThenBy(change => change.Bird.Id)
                .Take(PullBatchSize)
                .ToListAsync(cancellationToken);

            if (remoteChanges.Count == 0)
                return 0;

            var ids = remoteChanges.Select(change => change.Bird.Id).ToArray();
            var existingBirds = await localContext.Birds
                .AsNoTracking()
                .Where(bird => ids.Contains(bird.Id))
                .Select(bird => new
                {
                    Bird = bird,
                    SyncStamp = bird.UpdatedAt ?? bird.CreatedAt
                })
                .ToDictionaryAsync(bird => bird.Bird.Id, cancellationToken);

            var birdsToAdd = new List<Bird>();
            var birdsToUpdate = new List<Bird>();

            foreach (var change in remoteChanges)
            {
                if (existingBirds.TryGetValue(change.Bird.Id, out var localBird) &&
                    CompareStamps(localBird.SyncStamp, change.SyncStamp) > 0)
                {
                    continue;
                }

                var restored = Bird.Restore(
                    change.Bird.Id,
                    change.Bird.Name,
                    change.Bird.Description,
                    change.Bird.Arrival,
                    change.Bird.Departure,
                    change.Bird.IsAlive,
                    change.Bird.CreatedAt,
                    change.Bird.UpdatedAt);

                if (existingBirds.ContainsKey(change.Bird.Id))
                    birdsToUpdate.Add(restored);
                else
                    birdsToAdd.Add(restored);
            }

            if (birdsToAdd.Count > 0)
                await localContext.Birds.AddRangeAsync(birdsToAdd, cancellationToken);

            if (birdsToUpdate.Count > 0)
                localContext.Birds.UpdateRange(birdsToUpdate);

            var latestSyncStamp = remoteChanges[^1].SyncStamp;
            if (cursor is null)
            {
                cursor = RemoteSyncCursor.Create(BirdsPullCursorKey, latestSyncStamp);
                await localContext.RemoteSyncCursors.AddAsync(cursor, cancellationToken);
            }
            else
            {
                cursor.AdvanceTo(latestSyncStamp);
            }

            await localContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(LogMessages.RemotePullProcessed, remoteChanges.Count);
            return remoteChanges.Count;
        }

        private async Task<int> PullRemoteDeletesAsync(BirdDbContext localContext,
                                                       RemoteBirdDbContext remoteContext,
                                                       CancellationToken cancellationToken)
        {
            var cursor = await localContext.RemoteSyncCursors
                .SingleOrDefaultAsync(syncCursor => syncCursor.CursorKey == BirdDeletesPullCursorKey, cancellationToken);
            var watermarkUtc = cursor?.LastSyncedAtUtc;

            var remoteDeletes = await remoteContext.BirdTombstones
                .AsNoTracking()
                .Where(tombstone => watermarkUtc == null || tombstone.DeletedAtUtc > watermarkUtc.Value)
                .OrderBy(tombstone => tombstone.DeletedAtUtc)
                .ThenBy(tombstone => tombstone.BirdId)
                .Take(PullBatchSize)
                .ToListAsync(cancellationToken);

            if (remoteDeletes.Count == 0)
                return 0;

            var ids = remoteDeletes.Select(static tombstone => tombstone.BirdId).ToArray();
            var localBirds = await localContext.Birds
                .Where(bird => ids.Contains(bird.Id))
                .ToListAsync(cancellationToken);
            var localBirdMap = localBirds.ToDictionary(bird => bird.Id);
            var remoteCurrentBirds = await remoteContext.Birds
                .AsNoTracking()
                .Where(bird => ids.Contains(bird.Id))
                .Select(bird => new
                {
                    bird.Id,
                    SyncStamp = bird.UpdatedAt ?? bird.CreatedAt
                })
                .ToDictionaryAsync(bird => bird.Id, bird => bird.SyncStamp, cancellationToken);

            foreach (var tombstone in remoteDeletes)
            {
                if (!localBirdMap.TryGetValue(tombstone.BirdId, out var localBird))
                    continue;

                if (remoteCurrentBirds.TryGetValue(tombstone.BirdId, out var remoteStamp) &&
                    remoteStamp > tombstone.DeletedAtUtc)
                {
                    continue;
                }

                if (CompareStamps(GetSyncStamp(localBird), tombstone.DeletedAtUtc) > 0)
                {
                    continue;
                }

                if (localContext.Entry(localBird).State != EntityState.Deleted)
                    localContext.Birds.Remove(localBird);
            }

            var latestDeleteStamp = remoteDeletes[^1].DeletedAtUtc;
            if (cursor is null)
            {
                cursor = RemoteSyncCursor.Create(BirdDeletesPullCursorKey, latestDeleteStamp);
                await localContext.RemoteSyncCursors.AddAsync(cursor, cancellationToken);
            }
            else
            {
                cursor.AdvanceTo(latestDeleteStamp);
            }

            await localContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(LogMessages.RemotePullProcessed, remoteDeletes.Count);
            return remoteDeletes.Count;
        }

        private static async Task<int> ApplyPushBatchAsync(BirdDbContext localContext,
                                                           RemoteBirdDbContext remoteContext,
                                                           IReadOnlyCollection<SyncOperation> pendingOperations,
                                                           CancellationToken cancellationToken)
        {
            var remoteWinsCount = 0;
            var upserts = pendingOperations
                .Where(operation => operation.OperationType == SyncOperationType.Upsert)
                .Select(operation => JsonSerializer.Deserialize<BirdSyncPayload>(operation.PayloadJson)
                    ?? throw new InvalidOperationException("Failed to deserialize bird sync payload."))
                .ToList();

            var deletes = pendingOperations
                .Where(operation => operation.OperationType == SyncOperationType.Delete)
                .Select(operation => JsonSerializer.Deserialize<BirdDeleteSyncPayload>(operation.PayloadJson)
                    ?? throw new InvalidOperationException("Failed to deserialize bird delete sync payload."))
                .ToList();

            await using var transaction = await remoteContext.Database.BeginTransactionAsync(cancellationToken);

            if (upserts.Count > 0)
            {
                var ids = upserts.Select(static payload => payload.Id).ToArray();
                var existingRemoteBirds = await remoteContext.Birds
                    .AsNoTracking()
                    .Where(bird => ids.Contains(bird.Id))
                    .ToDictionaryAsync(bird => bird.Id, cancellationToken);
                var existingRemoteTombstones = await remoteContext.BirdTombstones
                    .Where(tombstone => ids.Contains(tombstone.BirdId))
                    .ToDictionaryAsync(tombstone => tombstone.BirdId, cancellationToken);
                var existingLocalBirds = await localContext.Birds
                    .Where(bird => ids.Contains(bird.Id))
                    .ToDictionaryAsync(bird => bird.Id, cancellationToken);
                var birdsToAdd = new List<Bird>();
                var birdsToUpdate = new List<Bird>();
                var tombstonesToRemove = new HashSet<Guid>();

                foreach (var payload in upserts)
                {
                    var localSyncStamp = GetSyncStamp(payload);

                    if (existingRemoteTombstones.TryGetValue(payload.Id, out var remoteTombstone) &&
                        CompareStamps(remoteTombstone.DeletedAtUtc, localSyncStamp) >= 0)
                    {
                        remoteWinsCount++;

                        if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                            localContext.Birds.Remove(localBird);

                        continue;
                    }

                    if (existingRemoteBirds.TryGetValue(payload.Id, out var remoteBird) &&
                        CompareStamps(GetSyncStamp(remoteBird), localSyncStamp) > 0)
                    {
                        remoteWinsCount++;

                        var restoredRemoteBird = RestoreBird(remoteBird);
                        if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                            localContext.Entry(localBird).CurrentValues.SetValues(restoredRemoteBird);
                        else
                            await localContext.Birds.AddAsync(restoredRemoteBird, cancellationToken);

                        continue;
                    }

                    var restoredLocalBird = RestoreBird(payload);

                    if (existingRemoteBirds.ContainsKey(payload.Id))
                        birdsToUpdate.Add(restoredLocalBird);
                    else
                        birdsToAdd.Add(restoredLocalBird);

                    tombstonesToRemove.Add(payload.Id);
                }

                if (birdsToAdd.Count > 0)
                    await remoteContext.Birds.AddRangeAsync(birdsToAdd, cancellationToken);

                if (birdsToUpdate.Count > 0)
                    remoteContext.Birds.UpdateRange(birdsToUpdate);

                if (birdsToAdd.Count > 0 || birdsToUpdate.Count > 0)
                    await remoteContext.SaveChangesAsync(cancellationToken);

                if (tombstonesToRemove.Count > 0)
                {
                    await remoteContext.BirdTombstones
                        .Where(tombstone => tombstonesToRemove.Contains(tombstone.BirdId))
                        .ExecuteDeleteAsync(cancellationToken);
                }
            }

            if (deletes.Count > 0)
            {
                var deleteIds = deletes.Select(static payload => payload.Id).ToArray();
                var existingRemoteBirds = await remoteContext.Birds
                    .AsNoTracking()
                    .Where(bird => deleteIds.Contains(bird.Id))
                    .ToDictionaryAsync(bird => bird.Id, cancellationToken);
                var existingTombstones = await remoteContext.BirdTombstones
                    .Where(tombstone => deleteIds.Contains(tombstone.BirdId))
                    .ToListAsync(cancellationToken);
                var existingLocalBirds = await localContext.Birds
                    .Where(bird => deleteIds.Contains(bird.Id))
                    .ToDictionaryAsync(bird => bird.Id, cancellationToken);
                var tombstoneMap = existingTombstones.ToDictionary(tombstone => tombstone.BirdId);
                var remoteDeleteIds = new HashSet<Guid>();

                foreach (var payload in deletes)
                {
                    var deleteStamp = NormalizeComparisonStamp(payload.DeletedAtUtc);

                    if (tombstoneMap.TryGetValue(payload.Id, out var existingTombstone) &&
                        CompareStamps(existingTombstone.DeletedAtUtc, deleteStamp) >= 0)
                    {
                        if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                            localContext.Birds.Remove(localBird);

                        continue;
                    }

                    if (existingRemoteBirds.TryGetValue(payload.Id, out var remoteBird) &&
                        CompareStamps(GetSyncStamp(remoteBird), deleteStamp) > 0)
                    {
                        remoteWinsCount++;

                        var restoredRemoteBird = RestoreBird(remoteBird);
                        if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                            localContext.Entry(localBird).CurrentValues.SetValues(restoredRemoteBird);
                        else
                            await localContext.Birds.AddAsync(restoredRemoteBird, cancellationToken);

                        continue;
                    }

                    remoteDeleteIds.Add(payload.Id);

                    if (tombstoneMap.TryGetValue(payload.Id, out var matchingTombstone))
                    {
                        matchingTombstone.AdvanceTo(payload.DeletedAtUtc);
                    }
                    else
                    {
                        await remoteContext.BirdTombstones.AddAsync(
                            RemoteBirdTombstone.Create(payload.Id, payload.DeletedAtUtc),
                            cancellationToken);
                    }
                }

                if (remoteDeleteIds.Count > 0)
                {
                    await remoteContext.Birds
                        .Where(bird => remoteDeleteIds.Contains(bird.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                if (remoteDeleteIds.Count > 0 || deletes.Count > 0)
                    await remoteContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return remoteWinsCount;
        }

        private static Bird RestoreBird(BirdSyncPayload payload)
        {
            if (!Enum.TryParse<BirdsName>(payload.Name, ignoreCase: true, out var parsedName))
                throw new InvalidOperationException($"Unknown bird species '{payload.Name}' in sync payload.");

            return Bird.Restore(
                payload.Id,
                parsedName,
                payload.Description,
                payload.Arrival,
                payload.Departure,
                payload.IsAlive,
                payload.CreatedAt,
                payload.UpdatedAt);
        }

        private static Bird RestoreBird(Bird bird)
            => Bird.Restore(
                bird.Id,
                bird.Name,
                bird.Description,
                bird.Arrival,
                bird.Departure,
                bird.IsAlive,
                bird.CreatedAt,
                bird.UpdatedAt);

        private static DateTime GetSyncStamp(Bird bird)
            => NormalizeComparisonStamp(bird.UpdatedAt ?? bird.CreatedAt);

        private static DateTime GetSyncStamp(BirdSyncPayload payload)
            => NormalizeComparisonStamp(payload.UpdatedAt ?? payload.CreatedAt);

        private static int CompareStamps(DateTime left, DateTime right)
            => NormalizeComparisonStamp(left).CompareTo(NormalizeComparisonStamp(right));

        private static DateTime NormalizeComparisonStamp(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
            };

        private static async Task EnsureRemoteSyncSchemaAsync(RemoteBirdDbContext remoteContext, CancellationToken cancellationToken)
        {
            var providerName = remoteContext.Database.ProviderName ?? string.Empty;
            var createTableSql = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                ? """
                  CREATE TABLE IF NOT EXISTS "BirdTombstones" (
                      "BirdId" uuid NOT NULL CONSTRAINT "PK_BirdTombstones" PRIMARY KEY,
                      "DeletedAtUtc" timestamp without time zone NOT NULL
                  );
                  """
                : """
                  CREATE TABLE IF NOT EXISTS "BirdTombstones" (
                      "BirdId" TEXT NOT NULL CONSTRAINT "PK_BirdTombstones" PRIMARY KEY,
                      "DeletedAtUtc" TEXT NOT NULL
                  );
                  """;

            await remoteContext.Database.ExecuteSqlRawAsync(
                createTableSql,
                cancellationToken);

            await remoteContext.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_BirdTombstones_DeletedAtUtc"
                ON "BirdTombstones" ("DeletedAtUtc");
                """,
                cancellationToken);
        }

        private async Task<RemoteSyncRunResult> HandleRemoteFailureAsync(BirdDbContext localContext,
                                                                         IReadOnlyCollection<SyncOperation> pendingOperations,
                                                                         RemoteSyncRunStatus status,
                                                                         string errorMessage,
                                                                         Exception? exception,
                                                                         CancellationToken cancellationToken)
        {
            if (pendingOperations.Count > 0)
            {
                MarkBatchFailed(pendingOperations, errorMessage, DateTime.UtcNow);
                await localContext.SaveChangesAsync(cancellationToken);
            }

            if (exception is not null)
                _logger.LogWarning(exception, LogMessages.RemoteSyncFailed, pendingOperations.Count);

            return new RemoteSyncRunResult(status, pendingOperations.Count, errorMessage);
        }

        private static bool IsBackendUnavailable(Exception exception)
        {
            if (exception is TimeoutException or OperationCanceledException)
                return true;

            return exception switch
            {
                NpgsqlException => true,
                DbUpdateException dbUpdateException when dbUpdateException.InnerException is not null
                    => IsBackendUnavailable(dbUpdateException.InnerException),
                _ when exception.InnerException is not null
                    => IsBackendUnavailable(exception.InnerException),
                _ => false
            };
        }

        private static void MarkBatchFailed(IEnumerable<SyncOperation> operations, string errorMessage, DateTime attemptUtc)
        {
            foreach (var operation in operations)
                operation.MarkFailed(errorMessage, attemptUtc);
        }
    }
}
