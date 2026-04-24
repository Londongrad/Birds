using System.Data;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Birds.Infrastructure.Services;

public sealed class RemoteSyncSchemaInitializer(ILogger<RemoteSyncSchemaInitializer> logger)
    : IRemoteSyncSchemaInitializer
{
    public const int CurrentSchemaVersion = 1;
    private const string CurrentSchemaDescription =
        "Initial remote sync schema with birds, tombstones, applied operations, and pull indexes.";

    private readonly ILogger<RemoteSyncSchemaInitializer> _logger = logger;

    public async Task InitializeAsync(RemoteBirdDbContext remoteContext, CancellationToken cancellationToken)
    {
        try
        {
            var providerName = remoteContext.Database.ProviderName ?? string.Empty;

            await EnsureRemoteSyncSchemaVersionTableAsync(remoteContext, providerName, cancellationToken);
            var currentVersionApplied = await IsCurrentSchemaVersionAppliedAsync(remoteContext, cancellationToken);
            await EnsureRemoteBirdsTableAsync(remoteContext, providerName, cancellationToken);
            await EnsureRemoteBirdTombstonesTableAsync(remoteContext, providerName, cancellationToken);
            await EnsureRemoteAppliedOperationsTableAsync(remoteContext, providerName, cancellationToken);
            await EnsureRemoteBirdSyncStampColumnAsync(remoteContext, providerName, cancellationToken);
            await EnsureRemoteIndexesAsync(remoteContext, cancellationToken);
            await RecordCurrentSchemaVersionAsync(remoteContext, providerName, currentVersionApplied, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            const string message = "Remote sync schema initialization failed.";
            _logger.LogError(ex, message);
            throw new RemoteSyncSchemaException(message, ex);
        }
    }

    private static Task EnsureRemoteSyncSchemaVersionTableAsync(RemoteBirdDbContext remoteContext,
        string providerName,
        CancellationToken cancellationToken)
    {
        var sql = IsPostgres(providerName)
            ? """
              CREATE TABLE IF NOT EXISTS "RemoteSyncSchemaVersions" (
                  "Version" integer NOT NULL CONSTRAINT "PK_RemoteSyncSchemaVersions" PRIMARY KEY,
                  "AppliedAtUtc" timestamp without time zone NOT NULL,
                  "Description" character varying(200) NULL
              );
              """
            : """
              CREATE TABLE IF NOT EXISTS "RemoteSyncSchemaVersions" (
                  "Version" INTEGER NOT NULL CONSTRAINT "PK_RemoteSyncSchemaVersions" PRIMARY KEY,
                  "AppliedAtUtc" TEXT NOT NULL,
                  "Description" TEXT NULL
              );
              """;

        return remoteContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static Task<bool> IsCurrentSchemaVersionAppliedAsync(RemoteBirdDbContext remoteContext,
        CancellationToken cancellationToken)
    {
        return remoteContext.RemoteSyncSchemaVersions
            .AsNoTracking()
            .AnyAsync(version => version.Version == CurrentSchemaVersion, cancellationToken);
    }

    private static Task EnsureRemoteBirdsTableAsync(RemoteBirdDbContext remoteContext,
        string providerName,
        CancellationToken cancellationToken)
    {
        var sql = IsPostgres(providerName)
            ? """
              CREATE TABLE IF NOT EXISTS "Birds" (
                  "Id" uuid NOT NULL CONSTRAINT "PK_Birds" PRIMARY KEY,
                  "Name" text NOT NULL,
                  "Description" character varying(200) NULL,
                  "Arrival" date NOT NULL,
                  "Departure" date NULL,
                  "IsAlive" boolean NOT NULL DEFAULT TRUE,
                  "CreatedAt" timestamp without time zone NOT NULL,
                  "UpdatedAt" timestamp without time zone NULL,
                  "SyncStampUtc" timestamp without time zone NOT NULL
              );
              """
            : """
              CREATE TABLE IF NOT EXISTS "Birds" (
                  "Id" TEXT NOT NULL CONSTRAINT "PK_Birds" PRIMARY KEY,
                  "Name" TEXT NOT NULL,
                  "Description" TEXT NULL,
                  "Arrival" TEXT NOT NULL,
                  "Departure" TEXT NULL,
                  "IsAlive" INTEGER NOT NULL DEFAULT 1,
                  "CreatedAt" TEXT NOT NULL,
                  "UpdatedAt" TEXT NULL,
                  "SyncStampUtc" TEXT NOT NULL
              );
              """;

        return remoteContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static Task EnsureRemoteBirdTombstonesTableAsync(RemoteBirdDbContext remoteContext,
        string providerName,
        CancellationToken cancellationToken)
    {
        var sql = IsPostgres(providerName)
            ? """
              CREATE TABLE IF NOT EXISTS "BirdTombstones" (
                  "BirdId" uuid NOT NULL CONSTRAINT "PK_BirdTombstones" PRIMARY KEY,
                  "DeletedAtUtc" timestamp without time zone NOT NULL
              );
              """
            : """
              CREATE TABLE IF NOT EXISTS "BirdTombstones" (
                  "BirdId" TEXT NOT NULL CONSTRAINT "PK_BirdTombstones" PRIMARY KEY,
                  "DeletedAtUtc" TEXT NOT NULL
              );
              """;

        return remoteContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static Task EnsureRemoteAppliedOperationsTableAsync(RemoteBirdDbContext remoteContext,
        string providerName,
        CancellationToken cancellationToken)
    {
        var sql = IsPostgres(providerName)
            ? """
              CREATE TABLE IF NOT EXISTS "AppliedSyncOperations" (
                  "OperationId" uuid NOT NULL CONSTRAINT "PK_AppliedSyncOperations" PRIMARY KEY,
                  "OperationType" text NOT NULL,
                  "EntityId" uuid NOT NULL,
                  "AppliedAtUtc" timestamp without time zone NOT NULL
              );
              """
            : """
              CREATE TABLE IF NOT EXISTS "AppliedSyncOperations" (
                  "OperationId" TEXT NOT NULL CONSTRAINT "PK_AppliedSyncOperations" PRIMARY KEY,
                  "OperationType" TEXT NOT NULL,
                  "EntityId" TEXT NOT NULL,
                  "AppliedAtUtc" TEXT NOT NULL
              );
              """;

        return remoteContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task EnsureRemoteBirdSyncStampColumnAsync(RemoteBirdDbContext remoteContext,
        string providerName,
        CancellationToken cancellationToken)
    {
        if (IsPostgres(providerName))
        {
            await remoteContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Birds"
                ADD COLUMN IF NOT EXISTS "SyncStampUtc" timestamp without time zone;
                """,
                cancellationToken);

            await remoteContext.Database.ExecuteSqlRawAsync(
                """
                UPDATE "Birds"
                SET "SyncStampUtc" = COALESCE(
                    "SyncStampUtc",
                    "UpdatedAt",
                    "CreatedAt",
                    CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
                WHERE "SyncStampUtc" IS NULL;
                """,
                cancellationToken);

            await remoteContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Birds"
                ALTER COLUMN "SyncStampUtc" SET NOT NULL;
                """,
                cancellationToken);

            return;
        }

        if (!await RemoteBirdColumnExistsAsync(remoteContext, "SyncStampUtc", cancellationToken))
            await remoteContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Birds"
                ADD COLUMN "SyncStampUtc" TEXT NULL;
                """,
                cancellationToken);

        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE "Birds"
            SET "SyncStampUtc" = COALESCE("SyncStampUtc", "UpdatedAt", "CreatedAt", strftime('%Y-%m-%d %H:%M:%f', 'now'))
            WHERE "SyncStampUtc" IS NULL;
            """,
            cancellationToken);
    }

    private static async Task EnsureRemoteIndexesAsync(RemoteBirdDbContext remoteContext,
        CancellationToken cancellationToken)
    {
        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_Birds_SyncStampUtc"
            ON "Birds" ("SyncStampUtc");
            """,
            cancellationToken);

        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_Birds_SyncStampUtc_Id"
            ON "Birds" ("SyncStampUtc", "Id");
            """,
            cancellationToken);

        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_BirdTombstones_DeletedAtUtc"
            ON "BirdTombstones" ("DeletedAtUtc");
            """,
            cancellationToken);

        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_BirdTombstones_DeletedAtUtc_BirdId"
            ON "BirdTombstones" ("DeletedAtUtc", "BirdId");
            """,
            cancellationToken);

        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_AppliedSyncOperations_EntityId"
            ON "AppliedSyncOperations" ("EntityId");
            """,
            cancellationToken);

        await remoteContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_AppliedSyncOperations_AppliedAtUtc"
            ON "AppliedSyncOperations" ("AppliedAtUtc");
            """,
            cancellationToken);
    }

    private async Task RecordCurrentSchemaVersionAsync(RemoteBirdDbContext remoteContext,
        string providerName,
        bool currentVersionApplied,
        CancellationToken cancellationToken)
    {
        if (currentVersionApplied)
        {
            _logger.LogDebug("Remote sync schema version {SchemaVersion} is already applied.",
                CurrentSchemaVersion);
            return;
        }

        var rowsAffected = IsPostgres(providerName)
            ? await remoteContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO "RemoteSyncSchemaVersions" ("Version", "AppliedAtUtc", "Description")
                 VALUES ({CurrentSchemaVersion}, CURRENT_TIMESTAMP AT TIME ZONE 'UTC', {CurrentSchemaDescription})
                 ON CONFLICT ("Version") DO NOTHING;
                 """,
                cancellationToken)
            : await remoteContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT OR IGNORE INTO "RemoteSyncSchemaVersions" ("Version", "AppliedAtUtc", "Description")
                 VALUES ({CurrentSchemaVersion}, strftime('%Y-%m-%d %H:%M:%f', 'now'), {CurrentSchemaDescription});
                 """,
                cancellationToken);

        if (rowsAffected > 0)
            _logger.LogInformation("Applied remote sync schema version {SchemaVersion}.", CurrentSchemaVersion);
    }

    private static async Task<bool> RemoteBirdColumnExistsAsync(RemoteBirdDbContext remoteContext,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = remoteContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await remoteContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Birds') WHERE name = $columnName;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$columnName";
            parameter.Value = columnName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            if (shouldClose)
                await remoteContext.Database.CloseConnectionAsync();
        }
    }

    private static bool IsPostgres(string providerName)
    {
        return providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
    }
}
