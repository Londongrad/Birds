using System.Data;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Birds.Infrastructure.Services;

public sealed class DatabaseInitializerService(
    IDbContextFactory<BirdDbContext> contextFactory,
    DatabaseRuntimeOptions options,
    RemoteSyncRuntimeOptions remoteSyncOptions,
    DatabaseSeedingOptions seedingOptions,
    BirdSeeder birdSeeder,
    ILogger<DatabaseInitializerService> logger) : IDatabaseInitializer
{
    private readonly BirdSeeder _birdSeeder = birdSeeder;
    private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;
    private readonly ILogger<DatabaseInitializerService> _logger = logger;
    private readonly DatabaseRuntimeOptions _options = options;
    private readonly RemoteSyncRuntimeOptions _remoteSyncOptions = remoteSyncOptions;
    private readonly DatabaseSeedingOptions _seedingOptions = seedingOptions;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            await InitializeLocalSchemaAsync(context, cancellationToken);

            var seedingOptions = GetSafeStartupSeedingOptions();
            if (seedingOptions.Mode != DatabaseSeedingMode.None)
                await _birdSeeder.SeedAsync(seedingOptions, cancellationToken);

            _logger.LogInformation("Local SQLite store is ready at {ConnectionString}.", _options.ConnectionString);

            if (_remoteSyncOptions.IsConfigured)
                _logger.LogInformation(
                    "Remote PostgreSQL sync backend is configured for future synchronization.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Local SQLite store initialization failed at {ConnectionString}.",
                _options.ConnectionString);
            throw;
        }
    }

    private async Task InitializeLocalSchemaAsync(BirdDbContext context, CancellationToken cancellationToken)
    {
        if (context.Database.IsSqlite()
            && await HasExistingSchemaWithoutMigrationHistoryAsync(context, cancellationToken))
            await BaselineExistingSqliteSchemaAsync(context, cancellationToken);

        await context.Database.MigrateAsync(cancellationToken);
    }

    private DatabaseSeedingOptions GetSafeStartupSeedingOptions()
    {
        if (_seedingOptions.Mode != DatabaseSeedingMode.RecreateAndSeed)
            return _seedingOptions;

        _logger.LogWarning(
            "Database seeding mode {Mode} does not recreate the local database during startup. Existing data is preserved.",
            DatabaseSeedingMode.RecreateAndSeed);

        return new DatabaseSeedingOptions(
            DatabaseSeedingMode.SeedIfEmpty,
            _seedingOptions.RecordCount,
            _seedingOptions.BatchSize,
            _seedingOptions.RandomSeed);
    }

    private static async Task<bool> HasExistingSchemaWithoutMigrationHistoryAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(context, "__EFMigrationsHistory", cancellationToken))
            return false;

        return await TableExistsAsync(context, "Birds", cancellationToken)
               || await TableExistsAsync(context, "SyncOperations", cancellationToken)
               || await TableExistsAsync(context, "RemoteSyncCursors", cancellationToken);
    }

    private static async Task BaselineExistingSqliteSchemaAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Older application versions used EnsureCreated plus targeted startup SQL, so those databases
        // have real user data and current tables but no EF migration history. Bring only missing
        // compatibility pieces into place, verify the expected shape, and then baseline the known
        // migrations so future startup can use MigrateAsync normally.
        await EnsureLegacyBirdSyncStampSchemaAsync(context, cancellationToken);
        await EnsureLegacyBirdVersionSchemaAsync(context, cancellationToken);
        await EnsureLegacySyncOutboxSchemaAsync(context, cancellationToken);
        await EnsureLegacyRemoteSyncCursorSchemaAsync(context, cancellationToken);
        await VerifyCurrentLocalSchemaAsync(context, cancellationToken);
        await EnsureMigrationHistoryTableAsync(context, cancellationToken);
        await MarkKnownMigrationsAppliedAsync(context, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureLegacySyncOutboxSchemaAsync(BirdDbContext context,
        CancellationToken cancellationToken)
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

    private static async Task EnsureLegacyBirdSyncStampSchemaAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(context, "Birds", cancellationToken))
            return;

        if (!await ColumnExistsAsync(context, "Birds", "SyncStampUtc", cancellationToken))
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Birds"
                ADD COLUMN "SyncStampUtc" TEXT NULL;
                """,
                cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE "Birds"
            SET "SyncStampUtc" = COALESCE("SyncStampUtc", "UpdatedAt", "CreatedAt", strftime('%Y-%m-%d %H:%M:%f', 'now'))
            WHERE "SyncStampUtc" IS NULL;
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_Birds_SyncStampUtc"
            ON "Birds" ("SyncStampUtc");
            """,
            cancellationToken);
    }

    private static async Task EnsureLegacyRemoteSyncCursorSchemaAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "RemoteSyncCursors" (
                "CursorKey" TEXT NOT NULL CONSTRAINT "PK_RemoteSyncCursors" PRIMARY KEY,
                "LastSyncedAtUtc" TEXT NULL,
                "LastSyncedEntityId" TEXT NULL
            );
            """,
            cancellationToken);

        if (!await ColumnExistsAsync(context, "RemoteSyncCursors", "LastSyncedEntityId", cancellationToken))
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "RemoteSyncCursors"
                ADD COLUMN "LastSyncedEntityId" TEXT NULL;
                """,
                cancellationToken);
    }

    private static async Task EnsureLegacyBirdVersionSchemaAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(context, "Birds", cancellationToken))
            return;

        if (!await ColumnExistsAsync(context, "Birds", "Version", cancellationToken))
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Birds"
                ADD COLUMN "Version" INTEGER NOT NULL DEFAULT 1;
                """,
                cancellationToken);
    }

    private static async Task VerifyCurrentLocalSchemaAsync(BirdDbContext context, CancellationToken cancellationToken)
    {
        await VerifyRequiredColumnsAsync(context, "Birds",
            [
                "Id",
                "Name",
                "Description",
                "Arrival",
                "Departure",
                "IsAlive",
                "CreatedAt",
                "UpdatedAt",
                "SyncStampUtc",
                "Version"
            ],
            cancellationToken);

        await VerifyRequiredColumnsAsync(context, "SyncOperations",
            [
                "Id",
                "AggregateType",
                "AggregateId",
                "OperationType",
                "PayloadJson",
                "CreatedAtUtc",
                "UpdatedAtUtc",
                "RetryCount",
                "LastAttemptAtUtc",
                "LastError"
            ],
            cancellationToken);

        await VerifyRequiredColumnsAsync(context, "RemoteSyncCursors",
            [
                "CursorKey",
                "LastSyncedAtUtc",
                "LastSyncedEntityId"
            ],
            cancellationToken);
    }

    private static async Task VerifyRequiredColumnsAsync(BirdDbContext context,
        string tableName,
        IReadOnlyCollection<string> requiredColumns,
        CancellationToken cancellationToken)
    {
        var columns = await LoadColumnNamesAsync(context, tableName, cancellationToken);
        if (columns.Count == 0)
            throw new InvalidOperationException($"Existing local database is missing required table '{tableName}'.");

        var missingColumns = requiredColumns
            .Where(column => !columns.Contains(column))
            .ToArray();

        if (missingColumns.Length > 0)
            throw new InvalidOperationException(
                $"Existing local database table '{tableName}' is missing required columns: {string.Join(", ", missingColumns)}.");
    }

    private static async Task EnsureMigrationHistoryTableAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """,
            cancellationToken);
    }

    private static async Task MarkKnownMigrationsAppliedAsync(BirdDbContext context,
        CancellationToken cancellationToken)
    {
        var productVersion = GetEfProductVersion();
        foreach (var migrationId in context.Database.GetMigrations())
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                 VALUES ({migrationId}, {productVersion});
                 """,
                cancellationToken);
    }

    private static string GetEfProductVersion()
    {
        return typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "9.0.0";
    }

    private static async Task<bool> TableExistsAsync(BirdDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            if (shouldClose)
                await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<bool> ColumnExistsAsync(BirdDbContext context,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var columns = await LoadColumnNamesAsync(context, tableName, cancellationToken);
        return columns.Contains(columnName);
    }

    private static async Task<HashSet<string>> LoadColumnNamesAsync(BirdDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT name FROM pragma_table_info('{tableName}');";
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                columns.Add(reader.GetString(0));

            return columns;
        }
        finally
        {
            if (shouldClose)
                await context.Database.CloseConnectionAsync();
        }
    }
}
