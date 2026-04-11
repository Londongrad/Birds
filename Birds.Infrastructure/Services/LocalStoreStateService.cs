using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Services;

public sealed class LocalStoreStateService(IDbContextFactory<BirdDbContext> contextFactory) : ILocalStoreStateService
{
    private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;

    public async Task<LocalStoreStateSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var birdCount = await context.Birds.CountAsync(cancellationToken);
        var pendingOperationCount = await context.SyncOperations.CountAsync(cancellationToken);

        return new LocalStoreStateSnapshot(birdCount, pendingOperationCount);
    }
}