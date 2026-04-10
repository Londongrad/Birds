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
        RemoteSyncRuntimeOptions remoteSyncOptions,
        DatabaseSeedingOptions seedingOptions,
        BirdSeeder birdSeeder,
        ILogger<DatabaseInitializerService> logger) : IDatabaseInitializer
    {
        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;
        private readonly DatabaseRuntimeOptions _options = options;
        private readonly RemoteSyncRuntimeOptions _remoteSyncOptions = remoteSyncOptions;
        private readonly DatabaseSeedingOptions _seedingOptions = seedingOptions;
        private readonly BirdSeeder _birdSeeder = birdSeeder;
        private readonly ILogger<DatabaseInitializerService> _logger = logger;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            if (_seedingOptions.Mode == DatabaseSeedingMode.RecreateAndSeed)
            {
                await context.Database.EnsureDeletedAsync(cancellationToken);
            }

            await context.Database.EnsureCreatedAsync(cancellationToken);
            await EnsureSyncOutboxSchemaAsync(context, cancellationToken);

            if (_seedingOptions.Mode != DatabaseSeedingMode.None)
                await _birdSeeder.SeedAsync(_seedingOptions, cancellationToken);

            _logger.LogInformation("Local SQLite store is ready at {ConnectionString}.", _options.ConnectionString);

            if (_remoteSyncOptions.IsConfigured)
            {
                _logger.LogInformation(
                    "Remote PostgreSQL sync backend is configured for future synchronization.");
            }
        }

        private static async Task EnsureSyncOutboxSchemaAsync(BirdDbContext context, CancellationToken cancellationToken)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "SyncOperations" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_SyncOperations" PRIMARY KEY,
                    "AggregateType" TEXT NOT NULL,
                    "AggregateId" TEXT NOT NULL,
                    "OperationType" TEXT NOT NULL,
                    "PayloadJson" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "UpdatedAtUtc" TEXT NOT NULL,
                    "RetryCount" INTEGER NOT NULL DEFAULT 0,
                    "LastAttemptAtUtc" TEXT NULL,
                    "LastError" TEXT NULL
                );
                """,
                cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SyncOperations_AggregateType_AggregateId"
                ON "SyncOperations" ("AggregateType", "AggregateId");
                """,
                cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_SyncOperations_CreatedAtUtc"
                ON "SyncOperations" ("CreatedAtUtc");
                """,
                cancellationToken);
        }
    }
}
