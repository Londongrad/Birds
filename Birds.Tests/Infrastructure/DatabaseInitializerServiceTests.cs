using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Seeding;
using Birds.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.Infrastructure
{
    public sealed class DatabaseInitializerServiceTests
    {
        [Fact]
        public async Task InitializeAsync_Should_Create_SyncOperations_Table_For_Existing_Local_Database()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"birds-sync-init-{Guid.NewGuid():N}.db");

            try
            {
                await CreateLegacyBirdsOnlySchemaAsync(databasePath);

                var options = new DbContextOptionsBuilder<BirdDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options;

                var contextFactory = new TestBirdDbContextFactory(options);
                var initializer = new DatabaseInitializerService(
                    contextFactory,
                    new DatabaseRuntimeOptions(DatabaseProvider.Sqlite, $"Data Source={databasePath}"),
                    RemoteSyncRuntimeOptions.Disabled,
                    new DatabaseSeedingOptions(DatabaseSeedingMode.None, 0, 1, 0),
                    new BirdSeeder(contextFactory, NullLogger<BirdSeeder>.Instance),
                    NullLogger<DatabaseInitializerService>.Instance);

                await initializer.InitializeAsync(CancellationToken.None);

                await using var connection = new SqliteConnection($"Data Source={databasePath}");
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
                                      SELECT COUNT(*)
                                      FROM sqlite_master
                                      WHERE type = 'table' AND name = 'SyncOperations';
                                      """;

                var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
                tableCount.Should().Be(1);
            }
            finally
            {
                try
                {
                    SqliteConnection.ClearAllPools();
                    if (File.Exists(databasePath))
                        File.Delete(databasePath);
                }
                catch (IOException)
                {
                    // Best-effort cleanup for a uniquely named temp database.
                }
            }
        }

        private static async Task CreateLegacyBirdsOnlySchemaAsync(string databasePath)
        {
            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                                  CREATE TABLE "Birds" (
                                      "Id" TEXT NOT NULL CONSTRAINT "PK_Birds" PRIMARY KEY,
                                      "Name" TEXT NOT NULL,
                                      "Description" TEXT NULL,
                                      "Arrival" TEXT NOT NULL,
                                      "Departure" TEXT NULL,
                                      "IsAlive" INTEGER NOT NULL DEFAULT 1,
                                      "CreatedAt" TEXT NOT NULL,
                                      "UpdatedAt" TEXT NULL
                                  );
                                  """;
            await command.ExecuteNonQueryAsync();
        }

        private sealed class TestBirdDbContextFactory(DbContextOptions<BirdDbContext> options)
            : IDbContextFactory<BirdDbContext>
        {
            public BirdDbContext CreateDbContext() => new(options);

            public ValueTask<BirdDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(CreateDbContext());
        }
    }
}
