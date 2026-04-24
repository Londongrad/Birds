using Birds.App.Services;
using Birds.Infrastructure.Configuration;
using Birds.Shared.Constants;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Birds.Tests.App.Services;

public sealed class DatabaseConfigurationResolverTests
{
    [Fact]
    public void Resolve_WhenUsingCurrentSqliteSetup_KeepsSqliteLocalAndDisablesRemoteSync()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:Sqlite"] = "Data Source=birds-local.db"
        });

        var result = DatabaseConfigurationResolver.Resolve(configuration);

        result.LocalStoreConnectionString.Should().Be("Data Source=birds-local.db");
        result.RemoteSync.IsEnabled.Should().BeFalse();
        result.RemoteSync.IsConfigured.Should().BeFalse();
        result.SeedingOptions.Mode.Should().Be(DatabaseSeedingMode.None);
    }

    [Fact]
    public void Resolve_WhenAppsettingsAreUsed_DisablesRemoteSyncByDefault()
    {
        var appSettingsPath = FindAppSettingsPath();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath)
            .Build();

        var result = DatabaseConfigurationResolver.Resolve(configuration);

        result.RemoteSync.IsEnabled.Should().BeFalse();
        result.RemoteSync.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenLegacyProviderIsPostgresWithoutExplicitRemoteSync_KeepsRemoteSyncDisabled()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Postgres",
            ["ConnectionStrings:Sqlite"] = "Data Source=birds-local.db",
            ["ConnectionStrings:Postgres"] = "Host=remote;Database=birds;Username=user;Password=secret"
        });

        var result = DatabaseConfigurationResolver.Resolve(configuration);

        result.LocalStoreConnectionString.Should().Be("Data Source=birds-local.db");
        result.RemoteSync.IsEnabled.Should().BeFalse();
        result.RemoteSync.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenRemoteSyncConfiguredExplicitly_UsesDedicatedConnectionNames()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:LocalStore:ConnectionStringName"] = "OfflineStore",
            ["Database:RemoteSync:Enabled"] = "true",
            ["Database:RemoteSync:ConnectionStringName"] = "SyncBackend",
            ["ConnectionStrings:OfflineStore"] = "Data Source=offline.db",
            ["ConnectionStrings:SyncBackend"] = "Host=sync;Database=birds_sync;Username=user;Password=secret"
        });

        var result = DatabaseConfigurationResolver.Resolve(configuration);

        result.LocalStoreConnectionString.Should().Be("Data Source=offline.db");
        result.RemoteSync.IsConfigured.Should().BeTrue();
        result.RemoteSync.ConnectionString.Should().Be("Host=sync;Database=birds_sync;Username=user;Password=secret");
    }

    [Fact]
    public void Resolve_WhenRemoteSyncEnabledButConnectionStringMissing_Should_ReportIncompleteConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:RemoteSync:Enabled"] = "true",
            ["Database:RemoteSync:ConnectionStringName"] = "SyncBackend",
            ["ConnectionStrings:Sqlite"] = "Data Source=offline.db"
        });

        var result = DatabaseConfigurationResolver.Resolve(configuration);

        result.RemoteSync.IsEnabled.Should().BeTrue();
        result.RemoteSync.IsConfigured.Should().BeFalse();
        result.RemoteSync.ConnectionString.Should().BeNull();
        result.RemoteSync.ConfigurationErrorMessage.Should().Be(ErrorMessages.RemoteSyncConfigurationMissing);
    }

    [Fact]
    public void Resolve_WhenRemoteSyncConnectionStringContainsUnresolvedPlaceholders_Should_ReportMissingVariables()
    {
        Environment.SetEnvironmentVariable("BIRDS_TEST_MISSING_DB_HOST", null);
        Environment.SetEnvironmentVariable("BIRDS_TEST_MISSING_DB_PASSWORD", null);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:RemoteSync:Enabled"] = "true",
            ["Database:RemoteSync:ConnectionStringName"] = "SyncBackend",
            ["ConnectionStrings:Sqlite"] = "Data Source=offline.db",
            ["ConnectionStrings:SyncBackend"] =
                "Host=${BIRDS_TEST_MISSING_DB_HOST};Database=birds;Username=user;Password=${BIRDS_TEST_MISSING_DB_PASSWORD}"
        });

        var result = DatabaseConfigurationResolver.Resolve(configuration);

        result.RemoteSync.IsEnabled.Should().BeTrue();
        result.RemoteSync.IsConfigured.Should().BeFalse();
        result.RemoteSync.ConnectionString.Should().BeNull();
        result.RemoteSync.ConfigurationErrorMessage.Should().Contain("BIRDS_TEST_MISSING_DB_HOST");
        result.RemoteSync.ConfigurationErrorMessage.Should().Contain("BIRDS_TEST_MISSING_DB_PASSWORD");
        result.RemoteSync.ConfigurationErrorMessage.Should().NotContain("Password=");
    }

    [Fact]
    public void Resolve_WhenRemoteSyncConnectionStringPlaceholdersAreResolved_Should_ConfigureRemoteSync()
    {
        try
        {
            Environment.SetEnvironmentVariable("BIRDS_TEST_RESOLVED_DB_HOST", "sync.example");
            Environment.SetEnvironmentVariable("BIRDS_TEST_RESOLVED_DB_PASSWORD", "secret");
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Database:RemoteSync:Enabled"] = "true",
                ["Database:RemoteSync:ConnectionStringName"] = "SyncBackend",
                ["ConnectionStrings:Sqlite"] = "Data Source=offline.db",
                ["ConnectionStrings:SyncBackend"] =
                    "Host=${BIRDS_TEST_RESOLVED_DB_HOST};Database=birds;Username=user;Password=${BIRDS_TEST_RESOLVED_DB_PASSWORD}"
            });

            var result = DatabaseConfigurationResolver.Resolve(configuration);

            result.RemoteSync.IsEnabled.Should().BeTrue();
            result.RemoteSync.IsConfigured.Should().BeTrue();
            result.RemoteSync.ConnectionString.Should()
                .Be("Host=sync.example;Database=birds;Username=user;Password=secret");
            result.RemoteSync.ConfigurationErrorMessage.Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("BIRDS_TEST_RESOLVED_DB_HOST", null);
            Environment.SetEnvironmentVariable("BIRDS_TEST_RESOLVED_DB_PASSWORD", null);
        }
    }

    [Fact]
    public void Resolve_WhenRemoteSyncConfigurationIsIncomplete_Should_NotLogRawConnectionStringSecrets()
    {
        var sink = new CollectingLogEventSink();
        var previousLogger = Log.Logger;
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(sink)
                .CreateLogger();
            Environment.SetEnvironmentVariable("BIRDS_TEST_MISSING_LOG_DB_HOST", null);
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Database:RemoteSync:Enabled"] = "true",
                ["Database:RemoteSync:ConnectionStringName"] = "SyncBackend",
                ["ConnectionStrings:Sqlite"] = "Data Source=offline.db",
                ["ConnectionStrings:SyncBackend"] =
                    "Host=${BIRDS_TEST_MISSING_LOG_DB_HOST};Database=birds;Username=user;Password=literal-secret"
            });

            _ = DatabaseConfigurationResolver.Resolve(configuration);

            var renderedEvents = string.Join(Environment.NewLine, sink.Events.Select(e => e.RenderMessage()));
            renderedEvents.Should().Contain("BIRDS_TEST_MISSING_LOG_DB_HOST");
            renderedEvents.Should().NotContain("literal-secret");
            renderedEvents.Should().NotContain("Password=");
        }
        finally
        {
            Environment.SetEnvironmentVariable("BIRDS_TEST_MISSING_LOG_DB_HOST", null);
            Log.Logger = previousLogger;
        }
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static string FindAppSettingsPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Birds.App", "appsettings.json");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Birds.App/appsettings.json.");
    }

    private sealed class CollectingLogEventSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
