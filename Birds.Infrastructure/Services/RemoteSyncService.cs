using System.Text.Json;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Birds.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Birds.Infrastructure.Services;

public sealed class RemoteSyncService(
    IDbContextFactory<BirdDbContext> localContextFactory,
    IDbContextFactory<RemoteBirdDbContext> remoteContextFactory,
    IRemoteSyncSchemaInitializer remoteSchemaInitializer,
    ILogger<RemoteSyncService> logger) : IRemoteSyncService
{
    private const int PushBatchSize = 128;
    private const int PullBatchSize = 128;
    private const string BirdsPullCursorKey = "Birds.Pull";
    private const string BirdDeletesPullCursorKey = "Birds.Deletes.Pull";

    private readonly IDbContextFactory<BirdDbContext> _localContextFactory = localContextFactory;
    private readonly ILogger<RemoteSyncService> _logger = logger;
    private readonly IDbContextFactory<RemoteBirdDbContext> _remoteContextFactory = remoteContextFactory;
    private readonly IRemoteSyncSchemaInitializer _remoteSchemaInitializer = remoteSchemaInitializer;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public async Task<RemoteSyncBackendCheckResult> CheckBackendAvailabilityAsync(CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            try
            {
                await using var remoteContext = await _remoteContextFactory.CreateDbContextAsync(cancellationToken);
                if (!await remoteContext.Database.CanConnectAsync(cancellationToken))
                    return new RemoteSyncBackendCheckResult(
                        RemoteSyncRunStatus.BackendUnavailable,
                        "Remote sync backend is unavailable.");

                await _remoteSchemaInitializer.InitializeAsync(remoteContext, cancellationToken);
                var remoteBirdCount = await remoteContext.Birds.CountAsync(cancellationToken);
                return new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Synced, null, remoteBirdCount);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                var status = ClassifyRemoteFailure(ex);

                _logger.LogWarning(ex, LogMessages.RemoteSyncFailed, 0);
                return new RemoteSyncBackendCheckResult(status, ex.Message);
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

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
                await _remoteSchemaInitializer.InitializeAsync(remoteContext, cancellationToken);

                if (pendingOperations.Count > 0)
                {
                    var pushResult = await PushPendingBatchAsync(localContext, remoteContext, pendingOperations,
                        cancellationToken);
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
            catch (LocalOutboxCleanupException ex) when (!cancellationToken.IsCancellationRequested)
            {
                return new RemoteSyncRunResult(
                    RemoteSyncRunStatus.Failed,
                    pendingOperations.Count,
                    ex.Message);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                var status = ClassifyRemoteFailure(ex);

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

    public async Task<RemoteSyncRunResult> UploadLocalSnapshotAsync(CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            await using var localContext = await _localContextFactory.CreateDbContextAsync(cancellationToken);
            try
            {
                await using var remoteContext = await _remoteContextFactory.CreateDbContextAsync(cancellationToken);
                if (!await remoteContext.Database.CanConnectAsync(cancellationToken))
                {
                    const string backendUnavailableMessage = "Remote sync backend is unavailable.";
                    return new RemoteSyncRunResult(
                        RemoteSyncRunStatus.BackendUnavailable,
                        0,
                        backendUnavailableMessage);
                }

                await _remoteSchemaInitializer.InitializeAsync(remoteContext, cancellationToken);

                var localBirds = await localContext.Birds
                    .AsNoTracking()
                    .OrderBy(bird => bird.SyncStampUtc)
                    .ThenBy(bird => bird.Id)
                    .ToListAsync(cancellationToken);

                await using (var remoteTransaction = await remoteContext.Database.BeginTransactionAsync(cancellationToken))
                {
                    await remoteContext.Birds.ExecuteDeleteAsync(cancellationToken);
                    await remoteContext.BirdTombstones.ExecuteDeleteAsync(cancellationToken);

                    if (localBirds.Count > 0)
                    {
                        var remoteSnapshot = localBirds
                            .Select(RestoreBird)
                            .ToList();

                        await remoteContext.Birds.AddRangeAsync(remoteSnapshot, cancellationToken);
                        await remoteContext.SaveChangesAsync(cancellationToken);
                    }

                    await remoteTransaction.CommitAsync(cancellationToken);
                }

                await localContext.SyncOperations.ExecuteDeleteAsync(cancellationToken);
                await localContext.RemoteSyncCursors.ExecuteDeleteAsync(cancellationToken);

                if (localBirds.Count > 0)
                {
                    var lastSyncedBird = localBirds[^1];

                    await localContext.RemoteSyncCursors.AddAsync(
                        RemoteSyncCursor.Create(BirdsPullCursorKey, GetSyncStamp(lastSyncedBird), lastSyncedBird.Id),
                        cancellationToken);
                    await localContext.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation(LogMessages.RemoteSyncProcessed, localBirds.Count);
                return new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, localBirds.Count);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                var status = ClassifyRemoteFailure(ex);

                _logger.LogWarning(ex, LogMessages.RemoteSyncFailed, 0);
                return new RemoteSyncRunResult(status, 0, ex.Message);
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
        var remoteWinsCount =
            await ApplyPushBatchAsync(localContext, remoteContext, pendingOperations, cancellationToken);

        try
        {
            localContext.SyncOperations.RemoveRange(pendingOperations);
            await localContext.SaveChangesAsync(cancellationToken);
            localContext.ChangeTracker.Clear();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            localContext.ChangeTracker.Clear();
            _logger.LogWarning(
                ex,
                "Remote sync push committed, but local outbox cleanup failed for {OperationCount} operations.",
                pendingOperations.Count);
            throw new LocalOutboxCleanupException(
                "Remote sync push committed, but local outbox cleanup failed. Pending operations will be retried safely.",
                ex);
        }

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
        var watermarkEntityId = cursor?.LastSyncedEntityId;

        var remoteChangesQuery = remoteContext.Birds
            .AsNoTracking()
            .Select(bird => new
            {
                Bird = bird,
                SyncStamp = bird.SyncStampUtc
            });

        if (watermarkUtc is { } syncedAtUtc)
        {
            remoteChangesQuery = watermarkEntityId is { } syncedEntityId
                ? remoteChangesQuery.Where(change =>
                    change.SyncStamp > syncedAtUtc ||
                    change.SyncStamp == syncedAtUtc && change.Bird.Id.CompareTo(syncedEntityId) > 0)
                : remoteChangesQuery.Where(change => change.SyncStamp >= syncedAtUtc);
        }

        var remoteChanges = await remoteChangesQuery
            .OrderBy(change => change.SyncStamp)
            .ThenBy(change => change.Bird.Id)
            .Take(PullBatchSize)
            .ToListAsync(cancellationToken);

        if (remoteChanges.Count == 0)
            return 0;

        var ids = remoteChanges.Select(change => change.Bird.Id).ToArray();
        var existingBirds = await localContext.Birds
            .Where(bird => ids.Contains(bird.Id))
            .ToDictionaryAsync(bird => bird.Id, cancellationToken);

        var birdsToAdd = new List<Bird>();

        foreach (var change in remoteChanges)
        {
            if (existingBirds.TryGetValue(change.Bird.Id, out var localBird) &&
                CompareStamps(GetSyncStamp(localBird), change.SyncStamp) > 0)
                continue;

            var restored = Bird.Restore(
                change.Bird.Id,
                change.Bird.Name,
                change.Bird.Description,
                change.Bird.Arrival,
                change.Bird.Departure,
                change.Bird.IsAlive,
                change.Bird.CreatedAt,
                change.Bird.UpdatedAt,
                change.Bird.SyncStampUtc);

            if (existingBirds.TryGetValue(change.Bird.Id, out var existingBird))
                ApplyExternalState(localContext, existingBird, restored);
            else
                birdsToAdd.Add(restored);
        }

        if (birdsToAdd.Count > 0)
            await localContext.Birds.AddRangeAsync(birdsToAdd, cancellationToken);

        var latestChange = remoteChanges[^1];
        if (cursor is null)
        {
            cursor = RemoteSyncCursor.Create(BirdsPullCursorKey, latestChange.SyncStamp, latestChange.Bird.Id);
            await localContext.RemoteSyncCursors.AddAsync(cursor, cancellationToken);
        }
        else
        {
            cursor.AdvanceTo(latestChange.SyncStamp, latestChange.Bird.Id);
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
        var watermarkEntityId = cursor?.LastSyncedEntityId;

        var remoteDeletesQuery = remoteContext.BirdTombstones
            .AsNoTracking();

        if (watermarkUtc is { } syncedAtUtc)
        {
            remoteDeletesQuery = watermarkEntityId is { } syncedEntityId
                ? remoteDeletesQuery.Where(tombstone =>
                    tombstone.DeletedAtUtc > syncedAtUtc ||
                    tombstone.DeletedAtUtc == syncedAtUtc && tombstone.BirdId.CompareTo(syncedEntityId) > 0)
                : remoteDeletesQuery.Where(tombstone => tombstone.DeletedAtUtc >= syncedAtUtc);
        }

        var remoteDeletes = await remoteDeletesQuery
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
                SyncStamp = bird.SyncStampUtc
            })
            .ToDictionaryAsync(bird => bird.Id, bird => bird.SyncStamp, cancellationToken);

        foreach (var tombstone in remoteDeletes)
        {
            if (!localBirdMap.TryGetValue(tombstone.BirdId, out var localBird))
                continue;

            if (remoteCurrentBirds.TryGetValue(tombstone.BirdId, out var remoteStamp) &&
                remoteStamp > tombstone.DeletedAtUtc)
                continue;

            if (CompareStamps(GetSyncStamp(localBird), tombstone.DeletedAtUtc) > 0) continue;

            if (localContext.Entry(localBird).State != EntityState.Deleted)
                localContext.Birds.Remove(localBird);
        }

        var latestDelete = remoteDeletes[^1];
        if (cursor is null)
        {
            cursor = RemoteSyncCursor.Create(BirdDeletesPullCursorKey, latestDelete.DeletedAtUtc,
                latestDelete.BirdId);
            await localContext.RemoteSyncCursors.AddAsync(cursor, cancellationToken);
        }
        else
        {
            cursor.AdvanceTo(latestDelete.DeletedAtUtc, latestDelete.BirdId);
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
            .Select(operation => new PendingUpsertOperation(
                operation,
                JsonSerializer.Deserialize<BirdSyncPayload>(operation.PayloadJson)
                ?? throw new InvalidOperationException("Failed to deserialize bird sync payload.")))
            .ToList();

        var deletes = pendingOperations
            .Where(operation => operation.OperationType == SyncOperationType.Delete)
            .Select(operation => new PendingDeleteOperation(
                operation,
                JsonSerializer.Deserialize<BirdDeleteSyncPayload>(operation.PayloadJson)
                ?? throw new InvalidOperationException("Failed to deserialize bird delete sync payload.")))
            .ToList();

        await using var transaction = await remoteContext.Database.BeginTransactionAsync(cancellationToken);

        var appliedOperationIds = await LoadAppliedOperationIdsAsync(remoteContext, pendingOperations, cancellationToken);
        upserts = upserts
            .Where(operation => !appliedOperationIds.Contains(operation.Operation.Id))
            .ToList();
        deletes = deletes
            .Where(operation => !appliedOperationIds.Contains(operation.Operation.Id))
            .ToList();

        var appliedAtUtc = DateTime.UtcNow;
        var newlyAppliedOperations = new List<RemoteAppliedSyncOperation>();

        if (upserts.Count > 0)
        {
            var ids = upserts.Select(static operation => operation.Payload.Id).ToArray();
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

            foreach (var operation in upserts)
            {
                var payload = operation.Payload;
                var localSyncStamp = GetSyncStamp(payload);

                if (existingRemoteTombstones.TryGetValue(payload.Id, out var remoteTombstone) &&
                    CompareStamps(remoteTombstone.DeletedAtUtc, localSyncStamp) >= 0)
                {
                    remoteWinsCount++;

                    if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                        localContext.Birds.Remove(localBird);

                    newlyAppliedOperations.Add(CreateAppliedOperation(operation.Operation, appliedAtUtc));
                    continue;
                }

                if (existingRemoteBirds.TryGetValue(payload.Id, out var remoteBird) &&
                    CompareStamps(GetSyncStamp(remoteBird), localSyncStamp) > 0)
                {
                    remoteWinsCount++;

                    var restoredRemoteBird = RestoreBird(remoteBird);
                    if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                        ApplyExternalState(localContext, localBird, restoredRemoteBird);
                    else
                        await localContext.Birds.AddAsync(restoredRemoteBird, cancellationToken);

                    newlyAppliedOperations.Add(CreateAppliedOperation(operation.Operation, appliedAtUtc));
                    continue;
                }

                var restoredLocalBird = RestoreBird(payload);

                if (existingRemoteBirds.ContainsKey(payload.Id))
                    birdsToUpdate.Add(restoredLocalBird);
                else
                    birdsToAdd.Add(restoredLocalBird);

                tombstonesToRemove.Add(payload.Id);
                newlyAppliedOperations.Add(CreateAppliedOperation(operation.Operation, appliedAtUtc));
            }

            if (birdsToAdd.Count > 0)
                await remoteContext.Birds.AddRangeAsync(birdsToAdd, cancellationToken);

            if (birdsToUpdate.Count > 0)
                remoteContext.Birds.UpdateRange(birdsToUpdate);

            if (birdsToAdd.Count > 0 || birdsToUpdate.Count > 0)
                await remoteContext.SaveChangesAsync(cancellationToken);

            if (tombstonesToRemove.Count > 0)
                await remoteContext.BirdTombstones
                    .Where(tombstone => tombstonesToRemove.Contains(tombstone.BirdId))
                    .ExecuteDeleteAsync(cancellationToken);
        }

        if (deletes.Count > 0)
        {
            var deleteIds = deletes.Select(static operation => operation.Payload.Id).ToArray();
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

            foreach (var operation in deletes)
            {
                var payload = operation.Payload;
                var deleteStamp = NormalizeComparisonStamp(payload.DeletedAtUtc);

                if (tombstoneMap.TryGetValue(payload.Id, out var existingTombstone) &&
                    CompareStamps(existingTombstone.DeletedAtUtc, deleteStamp) >= 0)
                {
                    if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                        localContext.Birds.Remove(localBird);

                    newlyAppliedOperations.Add(CreateAppliedOperation(operation.Operation, appliedAtUtc));
                    continue;
                }

                if (existingRemoteBirds.TryGetValue(payload.Id, out var remoteBird) &&
                    CompareStamps(GetSyncStamp(remoteBird), deleteStamp) > 0)
                {
                    remoteWinsCount++;

                    var restoredRemoteBird = RestoreBird(remoteBird);
                    if (existingLocalBirds.TryGetValue(payload.Id, out var localBird))
                        ApplyExternalState(localContext, localBird, restoredRemoteBird);
                    else
                        await localContext.Birds.AddAsync(restoredRemoteBird, cancellationToken);

                    newlyAppliedOperations.Add(CreateAppliedOperation(operation.Operation, appliedAtUtc));
                    continue;
                }

                remoteDeleteIds.Add(payload.Id);

                if (tombstoneMap.TryGetValue(payload.Id, out var matchingTombstone))
                    matchingTombstone.AdvanceTo(payload.DeletedAtUtc);
                else
                    await remoteContext.BirdTombstones.AddAsync(
                        RemoteBirdTombstone.Create(payload.Id, payload.DeletedAtUtc),
                        cancellationToken);

                newlyAppliedOperations.Add(CreateAppliedOperation(operation.Operation, appliedAtUtc));
            }

            if (remoteDeleteIds.Count > 0)
                await remoteContext.Birds
                    .Where(bird => remoteDeleteIds.Contains(bird.Id))
                    .ExecuteDeleteAsync(cancellationToken);

            if (remoteDeleteIds.Count > 0 || deletes.Count > 0)
                await remoteContext.SaveChangesAsync(cancellationToken);
        }

        if (newlyAppliedOperations.Count > 0)
        {
            await remoteContext.AppliedSyncOperations.AddRangeAsync(newlyAppliedOperations, cancellationToken);
            await remoteContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return remoteWinsCount;
    }

    private static async Task<HashSet<Guid>> LoadAppliedOperationIdsAsync(RemoteBirdDbContext remoteContext,
        IReadOnlyCollection<SyncOperation> pendingOperations,
        CancellationToken cancellationToken)
    {
        var operationIds = pendingOperations
            .Select(static operation => operation.Id)
            .ToArray();

        if (operationIds.Length == 0)
            return [];

        var appliedOperationIds = await remoteContext.AppliedSyncOperations
            .AsNoTracking()
            .Where(operation => operationIds.Contains(operation.OperationId))
            .Select(operation => operation.OperationId)
            .ToListAsync(cancellationToken);

        return appliedOperationIds.ToHashSet();
    }

    private static RemoteAppliedSyncOperation CreateAppliedOperation(SyncOperation operation, DateTime appliedAtUtc)
    {
        return RemoteAppliedSyncOperation.Create(
            operation.Id,
            operation.OperationType,
            operation.AggregateId,
            appliedAtUtc);
    }

    private static Bird RestoreBird(BirdSyncPayload payload)
    {
        var species = payload.Species ?? payload.Name;
        if (!BirdSpeciesCodes.TryParse(species, out var parsedName))
            throw new InvalidOperationException($"Unknown bird species '{species}' in sync payload.");

        return Bird.Restore(
            payload.Id,
            parsedName,
            payload.Description,
            payload.Arrival,
            payload.Departure,
            payload.IsAlive,
            payload.CreatedAt,
            payload.UpdatedAt,
            payload.SyncStampUtc);
    }

    private static Bird RestoreBird(Bird bird)
    {
        return Bird.Restore(
            bird.Id,
            bird.Name,
            bird.Description,
            bird.Arrival,
            bird.Departure,
            bird.IsAlive,
            bird.CreatedAt,
            bird.UpdatedAt,
            bird.SyncStampUtc);
    }

    private static void ApplyExternalState(BirdDbContext context, Bird target, Bird source)
    {
        var nextVersion = target.Version < Bird.InitialVersion
            ? Bird.InitialVersion
            : target.Version + 1;
        var entry = context.Entry(target);

        entry.CurrentValues.SetValues(source);
        entry.Property(bird => bird.Version).CurrentValue = nextVersion;
    }

    private static DateTime GetSyncStamp(Bird bird)
    {
        return NormalizeComparisonStamp(bird.SyncStampUtc);
    }

    private static DateTime GetSyncStamp(BirdSyncPayload payload)
    {
        return NormalizeComparisonStamp(payload.SyncStampUtc ?? payload.UpdatedAt ?? payload.CreatedAt);
    }

    private static int CompareStamps(DateTime left, DateTime right)
    {
        return NormalizeComparisonStamp(left).CompareTo(NormalizeComparisonStamp(right));
    }

    private static DateTime NormalizeComparisonStamp(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
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

    private static RemoteSyncRunStatus ClassifyRemoteFailure(Exception exception)
    {
        if (exception is RemoteSyncSchemaException)
            return RemoteSyncRunStatus.Failed;

        return IsBackendUnavailable(exception)
            ? RemoteSyncRunStatus.BackendUnavailable
            : RemoteSyncRunStatus.Failed;
    }

    private static void MarkBatchFailed(IEnumerable<SyncOperation> operations, string errorMessage, DateTime attemptUtc)
    {
        foreach (var operation in operations)
            operation.MarkFailed(errorMessage, attemptUtc);
    }

    private sealed class LocalOutboxCleanupException(string message, Exception innerException)
        : Exception(message, innerException);

    private readonly record struct PushBatchResult(int ProcessedCount, int RemoteWinsCount);

    private readonly record struct PendingUpsertOperation(SyncOperation Operation, BirdSyncPayload Payload);

    private readonly record struct PendingDeleteOperation(SyncOperation Operation, BirdDeleteSyncPayload Payload);
}
