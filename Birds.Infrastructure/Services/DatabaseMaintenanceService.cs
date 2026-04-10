using Birds.Application.Interfaces;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Birds.Infrastructure.Services
{
    public sealed class DatabaseMaintenanceService(
        IDbContextFactory<BirdDbContext> contextFactory) : IDatabaseMaintenanceService
    {
        private const string BirdAggregateType = "Bird";
        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;

        public bool CanResetLocalDatabase => true;

        public async Task<int> ClearBirdRecordsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var birdIds = await context.Birds
                .AsNoTracking()
                .Select(static bird => bird.Id)
                .ToListAsync(cancellationToken);

            if (birdIds.Count > 0)
            {
                var existingOperations = await context.SyncOperations
                    .Where(operation => operation.AggregateType == BirdAggregateType && birdIds.Contains(operation.AggregateId))
                    .ToDictionaryAsync(operation => operation.AggregateId, cancellationToken);

                var timestamp = DateTime.UtcNow;
                foreach (var birdId in birdIds)
                {
                    var payload = JsonSerializer.Serialize(new BirdDeleteSyncPayload(birdId, timestamp));
                    if (existingOperations.TryGetValue(birdId, out var existing))
                    {
                        existing.ReplacePendingPayload(SyncOperationType.Delete, payload, timestamp);
                    }
                    else
                    {
                        await context.SyncOperations.AddAsync(
                            SyncOperation.CreatePending(BirdAggregateType, birdId, SyncOperationType.Delete, payload, timestamp),
                            cancellationToken);
                    }
                }
            }

            var removed = await context.Birds.ExecuteDeleteAsync(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return removed;
        }

        public async Task ResetLocalDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Database.EnsureDeletedAsync(cancellationToken);
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        private sealed record BirdDeleteSyncPayload(Guid Id, DateTime DeletedAtUtc);
    }
}
