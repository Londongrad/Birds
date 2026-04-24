using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Birds.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.Infrastructure;

public sealed class RemoteSyncSchemaInitializerTests
{
    [Fact]
    public async Task InitializeAsync_WhenDatabaseIsFresh_Should_CreateRemoteSyncSchema()
    {
        await using var database = new RemoteSchemaTestDatabase();
        var sut = CreateSut();

        await using var context = database.CreateContext();
        await sut.InitializeAsync(context, CancellationToken.None);

        (await TableExistsAsync(database.Connection, "Birds")).Should().BeTrue();
        (await TableExistsAsync(database.Connection, "BirdTombstones")).Should().BeTrue();
        (await TableExistsAsync(database.Connection, "AppliedSyncOperations")).Should().BeTrue();
        (await TableExistsAsync(database.Connection, "RemoteSyncSchemaVersions")).Should().BeTrue();
        (await IndexExistsAsync(database.Connection, "IX_Birds_SyncStampUtc_Id")).Should().BeTrue();
        (await IndexExistsAsync(database.Connection, "IX_BirdTombstones_DeletedAtUtc_BirdId")).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WhenRunTwice_Should_BeIdempotent()
    {
        await using var database = new RemoteSchemaTestDatabase();
        var sut = CreateSut();

        await using (var context = database.CreateContext())
        {
            await sut.InitializeAsync(context, CancellationToken.None);
            await sut.InitializeAsync(context, CancellationToken.None);
        }

        await using var verifyContext = database.CreateContext();
        var versions = await verifyContext.RemoteSyncSchemaVersions.ToListAsync();
        versions.Should().ContainSingle();
        versions[0].Version.Should().Be(RemoteSyncSchemaInitializer.CurrentSchemaVersion);
    }

    [Fact]
    public async Task InitializeAsync_WhenExistingRemoteDataExists_Should_NotDeleteData()
    {
        await using var database = new RemoteSchemaTestDatabase();
        var sut = CreateSut();
        var bird = Bird.Restore(
            Guid.NewGuid(),
            BirdSpecies.Sparrow,
            "existing remote bird",
            new DateOnly(2026, 4, 20),
            null,
            true,
            DateTime.UtcNow.AddDays(-1),
            null,
            DateTime.UtcNow.AddDays(-1));

        await using (var context = database.CreateContext())
        {
            await sut.InitializeAsync(context, CancellationToken.None);
            await context.Birds.AddAsync(bird);
            await context.BirdTombstones.AddAsync(RemoteBirdTombstone.Create(Guid.NewGuid(), DateTime.UtcNow));
            await context.AppliedSyncOperations.AddAsync(
                RemoteAppliedSyncOperation.Create(Guid.NewGuid(), SyncOperationType.Upsert, bird.Id, DateTime.UtcNow));
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            await sut.InitializeAsync(context, CancellationToken.None);
        }

        await using var verifyContext = database.CreateContext();
        (await verifyContext.Birds.CountAsync()).Should().Be(1);
        (await verifyContext.BirdTombstones.CountAsync()).Should().Be(1);
        (await verifyContext.AppliedSyncOperations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task InitializeAsync_WhenLegacyBirdsTableMissesSyncStamp_Should_AddColumnAndPreserveRows()
    {
        await using var database = new RemoteSchemaTestDatabase();
        await CreateLegacyBirdsTableWithoutSyncStampAsync(database.Connection);
        var sut = CreateSut();

        await using var context = database.CreateContext();
        await sut.InitializeAsync(context, CancellationToken.None);

        (await ColumnExistsAsync(database.Connection, "Birds", "SyncStampUtc")).Should().BeTrue();
        (await ExecuteScalarAsync<long>(database.Connection, """SELECT COUNT(*) FROM "Birds";""")).Should().Be(1);
        var syncStamp = await ExecuteScalarAsync<string>(
            database.Connection,
            """SELECT "SyncStampUtc" FROM "Birds" LIMIT 1;""");
        syncStamp.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InitializeAsync_WhenApplied_Should_RecordSchemaVersion()
    {
        await using var database = new RemoteSchemaTestDatabase();
        var sut = CreateSut();

        await using var context = database.CreateContext();
        await sut.InitializeAsync(context, CancellationToken.None);

        var version = await context.RemoteSyncSchemaVersions.SingleAsync();
        version.Version.Should().Be(RemoteSyncSchemaInitializer.CurrentSchemaVersion);
        version.AppliedAtUtc.Should().NotBe(default);
        version.Description.Should().NotBeNullOrWhiteSpace();
    }

    private static RemoteSyncSchemaInitializer CreateSut()
    {
        return new RemoteSyncSchemaInitializer(NullLogger<RemoteSyncSchemaInitializer>.Instance);
    }

    private static async Task CreateLegacyBirdsTableWithoutSyncStampAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
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
                'legacy remote bird',
                '2026-04-20',
                NULL,
                1,
                '2026-04-20 08:00:00',
                NULL);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        return await ExecuteScalarAsync<long>(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;",
            ("$name", tableName)) > 0;
    }

    private static async Task<bool> IndexExistsAsync(SqliteConnection connection, string indexName)
    {
        return await ExecuteScalarAsync<long>(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name;",
            ("$name", indexName)) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
    {
        return await ExecuteScalarAsync<long>(
            connection,
            $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = $name;",
            ("$name", columnName)) > 0;
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
        result.Should().NotBeNull();
        return (T)Convert.ChangeType(result, typeof(T));
    }

    private sealed class RemoteSchemaTestDatabase : IAsyncDisposable
    {
        private readonly DbContextOptions<RemoteBirdDbContext> _options;

        public RemoteSchemaTestDatabase()
        {
            Connection = new SqliteConnection("Filename=:memory:");
            Connection.Open();
            _options = new DbContextOptionsBuilder<RemoteBirdDbContext>()
                .UseSqlite(Connection)
                .EnableSensitiveDataLogging()
                .Options;
        }

        public SqliteConnection Connection { get; }

        public RemoteBirdDbContext CreateContext()
        {
            return new RemoteBirdDbContext(_options);
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
        }
    }
}
