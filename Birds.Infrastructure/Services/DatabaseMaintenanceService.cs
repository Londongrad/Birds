using Birds.Application.Interfaces;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Services
{
    public sealed class DatabaseMaintenanceService(
        IDbContextFactory<BirdDbContext> contextFactory,
        DatabaseRuntimeOptions options) : IDatabaseMaintenanceService
    {
        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;
        private readonly DatabaseRuntimeOptions _options = options;

        public bool CanResetLocalDatabase => _options.Provider == DatabaseProvider.Sqlite;

        public async Task<int> ClearBirdRecordsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Birds.ExecuteDeleteAsync(cancellationToken);
        }

        public async Task ResetLocalDatabaseAsync(CancellationToken cancellationToken = default)
        {
            if (!CanResetLocalDatabase)
                throw new InvalidOperationException("Local database reset is only supported for SQLite.");

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Database.EnsureDeletedAsync(cancellationToken);
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
