using System.Text.Json;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Birds.Infrastructure.Repositories;

public class BirdRepository(
    IDbContextFactory<BirdDbContext> contextFactory,
    ILogger<BirdRepository>? logger = null) : IBirdRepository
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
    public async Task<Bird> UpdateAsync(
        Guid id,
        long expectedVersion,
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure,
        bool isAlive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var bird = await context.Birds
                       .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
                   ?? throw new NotFoundException(nameof(Bird), id);

        if (bird.Version != expectedVersion)
            throw CreateConcurrencyConflict(id);

        bird.Update(name, description, arrival, departure, isAlive);

        await QueueUpsertAsync(context, bird, cancellationToken);
        await SaveChangesWithConcurrencyHandlingAsync(context, id, cancellationToken);

        return bird;
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

        var existingBirds = await context.Birds
            .Where(bird => ids.Contains(bird.Id))
            .ToDictionaryAsync(bird => bird.Id, cancellationToken);

        var toAdd = new List<Bird>();
        var updatedCount = 0;
        foreach (var bird in birds)
        {
            if (existingBirds.TryGetValue(bird.Id, out var existing))
            {
                ApplyExternalState(context, existing, bird);
                updatedCount++;
            }
            else
            {
                toAdd.Add(bird);
            }
        }

        if (toAdd.Count > 0)
            await context.Birds.AddRangeAsync(toAdd, cancellationToken);

        await QueueUpsertsAsync(context, birds, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new UpsertBirdsResult(toAdd.Count, updatedCount);
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

        var existingBirds = ids.Length == 0
            ? new Dictionary<Guid, Bird>()
            : await context.Birds
                .Where(bird => ids.Contains(bird.Id))
                .ToDictionaryAsync(bird => bird.Id, cancellationToken);

        var toAdd = new List<Bird>();
        var updatedCount = 0;
        foreach (var bird in birds)
        {
            if (existingBirds.TryGetValue(bird.Id, out var existing))
            {
                ApplyExternalState(context, existing, bird);
                updatedCount++;
            }
            else
            {
                toAdd.Add(bird);
            }
        }

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

        await QueueUpsertsAsync(context, birds, cancellationToken);
        await QueueDeletesAsync(context, removedBirdIds, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UpsertBirdsResult(toAdd.Count, updatedCount, removed);
    }

    private static Task QueueUpsertAsync(BirdDbContext context, Bird bird, CancellationToken cancellationToken)
    {
        return QueueUpsertsAsync(context, [bird], cancellationToken);
    }

    private static void ApplyExternalState(BirdDbContext context, Bird target, Bird source)
    {
        var nextVersion = GetNextVersion(target.Version);
        var entry = context.Entry(target);

        entry.CurrentValues.SetValues(source);
        entry.Property(bird => bird.Version).CurrentValue = nextVersion;
    }

    private static long GetNextVersion(long currentVersion)
    {
        return currentVersion < Bird.InitialVersion
            ? Bird.InitialVersion
            : currentVersion + 1;
    }

    private async Task SaveChangesWithConcurrencyHandlingAsync(
        BirdDbContext context,
        Guid birdId,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw CreateConcurrencyConflict(birdId, ex);
        }
    }

    private ConcurrencyConflictException CreateConcurrencyConflict(
        Guid birdId,
        Exception? exception = null)
    {
        logger?.LogWarning(
            exception,
            "Optimistic concurrency conflict for bird {BirdId}.",
            birdId);

        return new ConcurrencyConflictException(nameof(Bird), birdId, exception);
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
