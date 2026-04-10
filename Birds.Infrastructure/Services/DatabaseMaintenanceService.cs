using Birds.Application.Interfaces;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Services
{
    public sealed class DatabaseMaintenanceService(
        IDbContextFactory<BirdDbContext> contextFactory) : IDatabaseMaintenanceService
    {
        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;

        public bool CanResetLocalDatabase => true;

        public async Task<int> ClearBirdRecordsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Birds.ExecuteDeleteAsync(cancellationToken);
        }

        public async Task ResetLocalDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Database.EnsureDeletedAsync(cancellationToken);
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
