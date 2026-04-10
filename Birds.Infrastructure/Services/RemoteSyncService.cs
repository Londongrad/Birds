using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Birds.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Birds.Infrastructure.Services
{
    public sealed class RemoteSyncService(
        IDbContextFactory<BirdDbContext> localContextFactory,
        IDbContextFactory<RemoteBirdDbContext> remoteContextFactory,
        ILogger<RemoteSyncService> logger) : IRemoteSyncService
    {
        private const int BatchSize = 128;
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
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                if (pendingOperations.Count == 0)
                    return RemoteSyncRunResult.NothingToSync;

                await using var remoteContext = await _remoteContextFactory.CreateDbContextAsync(cancellationToken);
                if (!await remoteContext.Database.CanConnectAsync(cancellationToken))
                {
                    MarkBatchFailed(pendingOperations, "Remote sync backend is unavailable.", DateTime.UtcNow);
                    await localContext.SaveChangesAsync(cancellationToken);
                    return new RemoteSyncRunResult(RemoteSyncRunStatus.BackendUnavailable, pendingOperations.Count);
                }

                try
                {
                    await ApplyBatchAsync(remoteContext, pendingOperations, cancellationToken);
                    localContext.SyncOperations.RemoveRange(pendingOperations);
                    await localContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(LogMessages.RemoteSyncProcessed, pendingOperations.Count);
                    return new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, pendingOperations.Count);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    MarkBatchFailed(pendingOperations, ex.Message, DateTime.UtcNow);
                    await localContext.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning(ex, LogMessages.RemoteSyncFailed, pendingOperations.Count);
                    return new RemoteSyncRunResult(RemoteSyncRunStatus.Failed, pendingOperations.Count);
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private static async Task ApplyBatchAsync(RemoteBirdDbContext remoteContext,
                                                  IReadOnlyCollection<SyncOperation> pendingOperations,
                                                  CancellationToken cancellationToken)
        {
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
                var existingIds = await remoteContext.Birds
                    .AsNoTracking()
                    .Where(bird => ids.Contains(bird.Id))
                    .Select(static bird => bird.Id)
                    .ToListAsync(cancellationToken);

                var existingSet = existingIds.ToHashSet();
                var birdsToAdd = new List<Bird>();
                var birdsToUpdate = new List<Bird>();

                foreach (var payload in upserts)
                {
                    if (!Enum.TryParse<BirdsName>(payload.Name, ignoreCase: true, out var parsedName))
                        throw new InvalidOperationException($"Unknown bird species '{payload.Name}' in sync payload.");

                    var restored = Bird.Restore(
                        payload.Id,
                        parsedName,
                        payload.Description,
                        payload.Arrival,
                        payload.Departure,
                        payload.IsAlive,
                        payload.CreatedAt,
                        payload.UpdatedAt);

                    if (existingSet.Contains(payload.Id))
                        birdsToUpdate.Add(restored);
                    else
                        birdsToAdd.Add(restored);
                }

                if (birdsToAdd.Count > 0)
                    await remoteContext.Birds.AddRangeAsync(birdsToAdd, cancellationToken);

                if (birdsToUpdate.Count > 0)
                    remoteContext.Birds.UpdateRange(birdsToUpdate);

                await remoteContext.SaveChangesAsync(cancellationToken);
            }

            if (deletes.Count > 0)
            {
                var deleteIds = deletes.Select(static payload => payload.Id).ToArray();
                await remoteContext.Birds
                    .Where(bird => deleteIds.Contains(bird.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        private static void MarkBatchFailed(IEnumerable<SyncOperation> operations, string errorMessage, DateTime attemptUtc)
        {
            foreach (var operation in operations)
                operation.MarkFailed(errorMessage, attemptUtc);
        }
    }
}
