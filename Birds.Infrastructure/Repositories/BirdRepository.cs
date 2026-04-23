using System.Text.Json;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Repositories;

public class BirdRepository(IDbContextFactory<BirdDbContext> contextFactory) : IBirdRepository
{
    private const string BirdAggregateType = "Bird";

    /// <inheritdoc />
    public async Task AddAsync(Bird bird, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Birds.AddAsync(bird, cancellationToken);
        await QueueUpsertAsync(context, bird, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Birds
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Birds
                   .AsNoTracking()
                   .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
               ?? throw new NotFoundException(nameof(Bird), id);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Bird bird, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Birds.Remove(bird);
        await QueueDeleteAsync(context, bird.Id, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Bird bird, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Birds.Update(bird);
        await QueueUpsertAsync(context, bird, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UpsertBirdsResult> UpsertAsync(
        IReadOnlyCollection<Bird> birds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(birds);

        if (birds.Count == 0)
            return new UpsertBirdsResult(0, 0);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var ids = birds
            .Select(static bird => bird.Id)
            .Distinct()
            .ToArray();

        var existingIds = await context.Birds
            .AsNoTracking()
            .Where(bird => ids.Contains(bird.Id))
            .Select(static bird => bird.Id)
            .ToListAsync(cancellationToken);

        var existingSet = existingIds.ToHashSet();
        var toAdd = birds.Where(bird => !existingSet.Contains(bird.Id)).ToList();
        var toUpdate = birds.Where(bird => existingSet.Contains(bird.Id)).ToList();

        if (toAdd.Count > 0)
            await context.Birds.AddRangeAsync(toAdd, cancellationToken);

        if (toUpdate.Count > 0)
            context.Birds.UpdateRange(toUpdate);

        await QueueUpsertsAsync(context, birds, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new UpsertBirdsResult(toAdd.Count, toUpdate.Count);
    }

    /// <inheritdoc />
    public async Task<UpsertBirdsResult> ReplaceWithSnapshotAsync(
        IReadOnlyCollection<Bird> birds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(birds);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var ids = birds
            .Select(static bird => bird.Id)
            .Distinct()
            .ToArray();

        var existingIds = ids.Length == 0
            ? Array.Empty<Guid>()
            : await context.Birds
                .AsNoTracking()
                .Where(bird => ids.Contains(bird.Id))
                .Select(static bird => bird.Id)
                .ToArrayAsync(cancellationToken);

        var existingSet = existingIds.ToHashSet();
        var toAdd = birds.Where(bird => !existingSet.Contains(bird.Id)).ToList();
        var toUpdate = birds.Where(bird => existingSet.Contains(bird.Id)).ToList();

        var removedBirdIds = ids.Length == 0
            ? await context.Birds
                .AsNoTracking()
                .Select(static bird => bird.Id)
                .ToListAsync(cancellationToken)
            : await context.Birds
                .AsNoTracking()
                .Where(bird => !ids.Contains(bird.Id))
                .Select(static bird => bird.Id)
                .ToListAsync(cancellationToken);

        var removed = ids.Length == 0
            ? await context.Birds.ExecuteDeleteAsync(cancellationToken)
            : await context.Birds
                .Where(bird => !ids.Contains(bird.Id))
                .ExecuteDeleteAsync(cancellationToken);

        if (toAdd.Count > 0)
            await context.Birds.AddRangeAsync(toAdd, cancellationToken);

        if (toUpdate.Count > 0)
            context.Birds.UpdateRange(toUpdate);

        await QueueUpsertsAsync(context, birds, cancellationToken);
        await QueueDeletesAsync(context, removedBirdIds, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UpsertBirdsResult(toAdd.Count, toUpdate.Count, removed);
    }

    private static Task QueueUpsertAsync(BirdDbContext context, Bird bird, CancellationToken cancellationToken)
    {
        return QueueUpsertsAsync(context, [bird], cancellationToken);
    }

    private static async Task QueueUpsertsAsync(BirdDbContext context,
        IReadOnlyCollection<Bird> birds,
        CancellationToken cancellationToken)
    {
        if (birds.Count == 0)
            return;

        var ids = birds.Select(static bird => bird.Id).Distinct().ToArray();
        var existingOperations = await LoadPendingOperationsAsync(context, ids, cancellationToken);
        var timestamp = DateTime.UtcNow;

        foreach (var bird in birds)
        {
            var payload = SerializeUpsertPayload(bird);
            if (existingOperations.TryGetValue(bird.Id, out var existing))
                existing.ReplacePendingPayload(SyncOperationType.Upsert, payload, timestamp);
            else
                await context.SyncOperations.AddAsync(
                    SyncOperation.CreatePending(BirdAggregateType, bird.Id, SyncOperationType.Upsert, payload,
                        timestamp),
                    cancellationToken);
        }
    }

    private static Task QueueDeleteAsync(BirdDbContext context, Guid birdId, CancellationToken cancellationToken)
    {
        return QueueDeletesAsync(context, [birdId], cancellationToken);
    }

    private static async Task QueueDeletesAsync(BirdDbContext context,
        IReadOnlyCollection<Guid> birdIds,
        CancellationToken cancellationToken)
    {
        if (birdIds.Count == 0)
            return;

        var existingOperations = await LoadPendingOperationsAsync(context, birdIds, cancellationToken);
        var timestamp = DateTime.UtcNow;

        foreach (var birdId in birdIds)
        {
            var payload = SerializeDeletePayload(birdId, timestamp);
            if (existingOperations.TryGetValue(birdId, out var existing))
                existing.ReplacePendingPayload(SyncOperationType.Delete, payload, timestamp);
            else
                await context.SyncOperations.AddAsync(
                    SyncOperation.CreatePending(BirdAggregateType, birdId, SyncOperationType.Delete, payload,
                        timestamp),
                    cancellationToken);
        }
    }

    private static Task<Dictionary<Guid, SyncOperation>> LoadPendingOperationsAsync(BirdDbContext context,
        IReadOnlyCollection<Guid> birdIds,
        CancellationToken cancellationToken)
    {
        return context.SyncOperations
            .Where(operation => operation.AggregateType == BirdAggregateType && birdIds.Contains(operation.AggregateId))
            .ToDictionaryAsync(operation => operation.AggregateId, cancellationToken);
    }

    private static string SerializeUpsertPayload(Bird bird)
    {
        var payload = new BirdSyncPayload(
            bird.Id,
            bird.Name.ToString(),
            bird.Name.ToString(),
            bird.Description,
            bird.Arrival,
            bird.Departure,
            bird.IsAlive,
            bird.CreatedAt,
            bird.UpdatedAt,
            bird.SyncStampUtc);

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeDeletePayload(Guid birdId, DateTime deletedAtUtc)
    {
        return JsonSerializer.Serialize(new BirdDeleteSyncPayload(birdId, deletedAtUtc));
    }
}
