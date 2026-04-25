using System.IO;
using System.Configuration;
using Birds.Infrastructure.Configuration;
using Birds.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Birds.App.Services;

internal static class DatabaseConfigurationResolver
{
    private const string DefaultRemotePostgresConnectionString =
        "Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}";

    internal static DatabaseStartupConfiguration Resolve(IConfiguration configuration)
    {
        var legacyProvider = ResolveLegacyProvider(configuration);
        var localStoreConnectionString = ResolveLocalStoreConnectionString(configuration, legacyProvider);
        var seedingOptions = ResolveSeedingOptions(configuration);
        var remoteSync = ResolveRemoteSyncOptions(configuration, legacyProvider);

        return new DatabaseStartupConfiguration(localStoreConnectionString, seedingOptions, remoteSync);
    }

    private static DatabaseProvider ResolveLegacyProvider(IConfiguration configuration)
    {
        var configuredProvider = configuration["Database:Provider"];
        if (string.IsNullOrWhiteSpace(configuredProvider))
            return DatabaseProvider.Sqlite;

        if (Enum.TryParse<DatabaseProvider>(configuredProvider, true, out var provider))
            return provider;

        throw new ConfigurationErrorsException(ErrorMessages.InvalidDatabaseProvider(configuredProvider));
    }

    private static string ResolveLocalStoreConnectionString(IConfiguration configuration,
        DatabaseProvider legacyProvider)
    {
        var configuredConnectionName = configuration["Database:LocalStore:ConnectionStringName"];

        if (string.IsNullOrWhiteSpace(configuredConnectionName) && legacyProvider != DatabaseProvider.Postgres)
            configuredConnectionName = configuration["Database:ConnectionStringName"];

        var resolvedConnection = TryResolveConnectionString(configuration, configuredConnectionName, "Sqlite",
            "DefaultConnection");
        if (resolvedConnection.ConnectionString is not null)
            return resolvedConnection.ConnectionString;

        if (string.IsNullOrWhiteSpace(configuredConnectionName))
            return CreateDefaultLocalStoreConnectionString();

        throw new ConfigurationErrorsException(
            ErrorMessages.ConnectionStringNotFoundFor(resolvedConnection.Names.ToArray()));
    }

    private static RemoteSyncRuntimeOptions ResolveRemoteSyncOptions(IConfiguration configuration,
        DatabaseProvider legacyProvider)
    {
        var explicitlyEnabled = configuration.GetValue<bool?>("REMOTE_SYNC_ENABLED")
                                ?? configuration.GetValue<bool?>("Database:RemoteSync:Enabled");
        var isEnabled = explicitlyEnabled ?? false;

        if (!isEnabled)
            return RemoteSyncRuntimeOptions.Disabled;

        var configuredConnectionName = configuration["Database:RemoteSync:ConnectionStringName"];

        if (string.IsNullOrWhiteSpace(configuredConnectionName) && legacyProvider == DatabaseProvider.Postgres)
            configuredConnectionName = configuration["Database:ConnectionStringName"];

        var resolvedConnection =
            TryResolveConnectionString(configuration, configuredConnectionName, "Postgres", "DefaultConnection");
        if (resolvedConnection.ConnectionString is null && string.IsNullOrWhiteSpace(configuredConnectionName))
        {
            resolvedConnection = new ResolvedConnectionString(
                App.ReplaceEnvPlaceholders(DefaultRemotePostgresConnectionString),
                "Postgres environment defaults",
                resolvedConnection.Names);
        }

        if (resolvedConnection.ConnectionString is null)
        {
            Log.Warning("Remote PostgreSQL sync is enabled, but no remote connection string was found.");
            return RemoteSyncRuntimeOptions.EnabledButNotConfigured(ErrorMessages.RemoteSyncConfigurationMissing);
        }

        var unresolvedPlaceholders = App.FindEnvPlaceholders(resolvedConnection.ConnectionString);
        if (unresolvedPlaceholders.Count > 0)
        {
            var placeholderList = string.Join(", ", unresolvedPlaceholders);
            Log.Warning(
                "Remote PostgreSQL sync is enabled, but connection string '{ConnectionStringName}' has unresolved environment variables: {EnvironmentVariables}.",
                resolvedConnection.Name,
                placeholderList);
            return RemoteSyncRuntimeOptions.EnabledButNotConfigured(
                ErrorMessages.RemoteSyncMissingEnvironmentVariables(placeholderList));
        }

        return new RemoteSyncRuntimeOptions(true, resolvedConnection.ConnectionString);
    }

    private static string ResolveConnectionString(IConfiguration configuration,
        string? configuredConnectionName,
        params string[] fallbackNames)
    {
        var resolvedConnection = TryResolveConnectionString(configuration, configuredConnectionName, fallbackNames);
        if (resolvedConnection.ConnectionString is not null)
            return resolvedConnection.ConnectionString;

        throw new ConfigurationErrorsException(
            ErrorMessages.ConnectionStringNotFoundFor(resolvedConnection.Names.ToArray()));
    }

    private static string CreateDefaultLocalStoreConnectionString()
    {
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Birds",
            "birds.db");

        return $"Data Source={databasePath}";
    }

    private static ResolvedConnectionString TryResolveConnectionString(IConfiguration configuration,
        string? configuredConnectionName,
        params string[] fallbackNames)
    {
        var candidateNames = string.IsNullOrWhiteSpace(configuredConnectionName)
            ? fallbackNames
            : [configuredConnectionName];

        foreach (var name in candidateNames)
        {
            var rawConnection = configuration.GetConnectionString(name);
            if (!string.IsNullOrWhiteSpace(rawConnection))
                return new ResolvedConnectionString(App.ReplaceEnvPlaceholders(rawConnection), name, candidateNames);
        }

        return new ResolvedConnectionString(null, null, candidateNames);
    }

    private static DatabaseSeedingOptions ResolveSeedingOptions(IConfiguration configuration)
    {
        var configuredMode = configuration["Seeding:Mode"];
        var mode = string.IsNullOrWhiteSpace(configuredMode)
            ? DatabaseSeedingMode.None
            : Enum.TryParse<DatabaseSeedingMode>(configuredMode, true, out var parsedMode)
                ? parsedMode
                : throw new ConfigurationErrorsException(ErrorMessages.InvalidDatabaseSeedingMode(configuredMode));

        var recordCount = Math.Max(0, configuration.GetValue<int?>("Seeding:RecordCount") ?? 20_000);
        var batchSize = Math.Max(1, configuration.GetValue<int?>("Seeding:BatchSize") ?? 500);
        var randomSeed = Math.Max(0, configuration.GetValue<int?>("Seeding:RandomSeed") ?? 42);

        return new DatabaseSeedingOptions(mode, recordCount, batchSize, randomSeed);
    }
}

internal sealed record DatabaseStartupConfiguration(
    string LocalStoreConnectionString,
    DatabaseSeedingOptions SeedingOptions,
    RemoteSyncRuntimeOptions RemoteSync);

internal sealed record ResolvedConnectionString(
    string? ConnectionString,
    string? Name,
    IReadOnlyList<string> Names);
