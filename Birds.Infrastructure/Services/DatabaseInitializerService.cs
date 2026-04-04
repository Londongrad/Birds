using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Birds.Infrastructure.Services
{
    public sealed class DatabaseInitializerService(
        IDbContextFactory<BirdDbContext> contextFactory,
        DatabaseRuntimeOptions options,
        DatabaseSeedingOptions seedingOptions,
        BirdSeeder birdSeeder,
        ILogger<DatabaseInitializerService> logger) : IDatabaseInitializer
    {
        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;
        private readonly DatabaseRuntimeOptions _options = options;
        private readonly DatabaseSeedingOptions _seedingOptions = seedingOptions;
        private readonly BirdSeeder _birdSeeder = birdSeeder;
        private readonly ILogger<DatabaseInitializerService> _logger = logger;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            switch (_options.Provider)
            {
                case DatabaseProvider.Sqlite:
                    if (_seedingOptions.Mode == DatabaseSeedingMode.RecreateAndSeed)
                    {
                        await context.Database.EnsureDeletedAsync(cancellationToken);
                    }

                    await context.Database.EnsureCreatedAsync(cancellationToken);

                    if (_seedingOptions.Mode != DatabaseSeedingMode.None)
                        await _birdSeeder.SeedAsync(_seedingOptions, cancellationToken);

                    _logger.LogInformation("SQLite database is ready at {ConnectionString}.", _options.ConnectionString);
                    break;

                case DatabaseProvider.Postgres:
                    if (await context.Database.CanConnectAsync(cancellationToken))
                    {
                        await context.Database.MigrateAsync(cancellationToken);
                        _logger.LogInformation("PostgreSQL database is reachable and migrations are applied.");
                    }

                    if (_seedingOptions.Mode != DatabaseSeedingMode.None)
                    {
                        _logger.LogWarning(
                            "Database seeding mode {Mode} was requested, but runtime seeding is only supported for SQLite. Skipping.",
                            _seedingOptions.Mode);
                    }

                    break;
            }
        }
    }
}
