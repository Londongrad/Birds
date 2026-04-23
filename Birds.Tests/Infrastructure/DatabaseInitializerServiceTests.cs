using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Seeding;
using Birds.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.Infrastructure;

public sealed class DatabaseInitializerServiceTests
{
    [Fact]
    public async Task InitializeAsync_Should_Create_New_Local_Database_Through_Migrations()
    {
        var databasePath = CreateTempDatabasePath("birds-migration-init");

        try
        {
            var initializer = CreateInitializer(databasePath);

            await initializer.InitializeAsync(CancellationToken.None);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();

            (await TableExistsAsync(connection, "__EFMigrationsHistory")).Should().BeTrue();
            (await TableExistsAsync(connection, "Birds")).Should().BeTrue();
            (await TableExistsAsync(connection, "SyncOperations")).Should().BeTrue();
            (await TableExistsAsync(connection, "RemoteSyncCursors")).Should().BeTrue();

            var appliedMigrationCount = await ExecuteScalarAsync<long>(
                connection,
                """SELECT COUNT(*) FROM "__EFMigrationsHistory";""");
            appliedMigrationCount.Should().BeGreaterThan(0);
        }
        finally
        {
            DeleteTempDatabase(databasePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_Should_Create_Sync_Metadata_Tables_For_Existing_Local_Database()
    {
        var databasePath = CreateTempDatabasePath("birds-sync-init");

        try
        {
            await CreateLegacyBirdsOnlySchemaAsync(databasePath);
            await InsertLegacyBirdAsync(databasePath);

            var initializer = CreateInitializer(databasePath);

            await initializer.InitializeAsync(CancellationToken.None);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                                  SELECT COUNT(*)
                                  FROM sqlite_master
                                  WHERE type = 'table' AND name IN ('SyncOperations', 'RemoteSyncCursors');
                                  """;

            var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            tableCount.Should().Be(2);

            command.CommandText = """
                                  SELECT COUNT(*)
                                  FROM pragma_table_info('Birds')
                                  WHERE name = 'SyncStampUtc';
                                  """;

            var syncStampColumnCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            syncStampColumnCount.Should().Be(1);

            command.CommandText = """
                                  SELECT COUNT(*)
                                  FROM pragma_table_info('RemoteSyncCursors')
                                  WHERE name = 'LastSyncedEntityId';
                                  """;

            var cursorEntityIdColumnCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            cursorEntityIdColumnCount.Should().Be(1);

            command.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsHistory";""";
            var appliedMigrationCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            appliedMigrationCount.Should().BeGreaterThan(0);

            command.CommandText = """SELECT COUNT(*) FROM "Birds";""";
            var birdCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            birdCount.Should().Be(1);
        }
        finally
        {
            DeleteTempDatabase(databasePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_When_Migration_Fails_Should_Log_And_Rethrow()
    {
        var logger = new CapturingLogger<DatabaseInitializerService>();
        var failingFactory = new FailingBirdDbContextFactory();
        var initializer = new DatabaseInitializerService(
            failingFactory,
            new DatabaseRuntimeOptions(DatabaseProvider.Sqlite, "Data Source=failing.db"),
            RemoteSyncRuntimeOptions.Disabled,
            new DatabaseSeedingOptions(DatabaseSeedingMode.None, 0, 1, 0),
            new BirdSeeder(failingFactory, NullLogger<BirdSeeder>.Instance),
            logger);

        var act = async () => await initializer.InitializeAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("migration failed");

        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("Local SQLite store initialization failed", StringComparison.Ordinal));
    }

    private static DatabaseInitializerService CreateInitializer(string databasePath)
    {
        var options = new DbContextOptionsBuilder<BirdDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        var contextFactory = new TestBirdDbContextFactory(options);
        return new DatabaseInitializerService(
            contextFactory,
            new DatabaseRuntimeOptions(DatabaseProvider.Sqlite, $"Data Source={databasePath}"),
            RemoteSyncRuntimeOptions.Disabled,
            new DatabaseSeedingOptions(DatabaseSeedingMode.None, 0, 1, 0),
            new BirdSeeder(contextFactory, NullLogger<BirdSeeder>.Instance),
            NullLogger<DatabaseInitializerService>.Instance);
    }

    private static string CreateTempDatabasePath(string prefix)
    {
        return Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}.db");
    }

    private static void DeleteTempDatabase(string databasePath)
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

    private static async Task InsertLegacyBirdAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO "Birds" (
                                  "Id",
                                  "Name",
                                  "Description",
                                  "Arrival",
                                  "Departure",
                                  "IsAlive",
                                  "CreatedAt",
                                  "UpdatedAt")
                              VALUES (
                                  '00000000-0000-0000-0000-000000000001',
                                  'Sparrow',
                                  'existing bird',
                                  '2026-04-01',
                                  NULL,
                                  1,
                                  '2026-04-01 08:00:00',
                                  NULL);
                              """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        var count = await ExecuteScalarAsync<long>(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;",
            ("$tableName", tableName));

        return count > 0;
    }

    private static async Task<T> ExecuteScalarAsync<T>(SqliteConnection connection,
        string commandText,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    private sealed class TestBirdDbContextFactory(DbContextOptions<BirdDbContext> options)
        : IDbContextFactory<BirdDbContext>
    {
        public BirdDbContext CreateDbContext()
        {
            return new BirdDbContext(options);
        }

        public ValueTask<BirdDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(CreateDbContext());
        }
    }

    private sealed class FailingBirdDbContextFactory : IDbContextFactory<BirdDbContext>
    {
        public BirdDbContext CreateDbContext()
        {
            throw new InvalidOperationException("migration failed");
        }

        public ValueTask<BirdDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("migration failed");
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
