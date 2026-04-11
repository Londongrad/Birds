using System.Configuration;
using Birds.Infrastructure.Configuration;
using Birds.Shared.Constants;
using Microsoft.Extensions.Configuration;

namespace Birds.App.Services;

internal static class DatabaseConfigurationResolver
{
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

        return ResolveConnectionString(configuration, configuredConnectionName, "Sqlite", "DefaultConnection");
    }

    private static RemoteSyncRuntimeOptions ResolveRemoteSyncOptions(IConfiguration configuration,
        DatabaseProvider legacyProvider)
    {
        var explicitlyEnabled = configuration.GetValue<bool?>("Database:RemoteSync:Enabled");
        var isEnabled = explicitlyEnabled ?? legacyProvider == DatabaseProvider.Postgres;

        if (!isEnabled)
            return RemoteSyncRuntimeOptions.Disabled;

        var configuredConnectionName = configuration["Database:RemoteSync:ConnectionStringName"];

        if (string.IsNullOrWhiteSpace(configuredConnectionName) && legacyProvider == DatabaseProvider.Postgres)
            configuredConnectionName = configuration["Database:ConnectionStringName"];

        var connectionString =
            ResolveConnectionString(configuration, configuredConnectionName, "Postgres", "DefaultConnection");
        return new RemoteSyncRuntimeOptions(true, connectionString);
    }

    private static string ResolveConnectionString(IConfiguration configuration,
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
                return App.ReplaceEnvPlaceholders(rawConnection);
        }

        throw new ConfigurationErrorsException(ErrorMessages.ConnectionStringNotFoundFor(candidateNames));
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