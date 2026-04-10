using Birds.App.Services;
using Birds.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Birds.Tests.App.Services
{
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
        public void Resolve_WhenLegacyProviderIsPostgres_UsesSqliteLocallyAndConfiguresRemoteSync()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:Sqlite"] = "Data Source=birds-local.db",
                ["ConnectionStrings:Postgres"] = "Host=remote;Database=birds;Username=user;Password=secret"
            });

            var result = DatabaseConfigurationResolver.Resolve(configuration);

            result.LocalStoreConnectionString.Should().Be("Data Source=birds-local.db");
            result.RemoteSync.IsEnabled.Should().BeTrue();
            result.RemoteSync.IsConfigured.Should().BeTrue();
            result.RemoteSync.ConnectionString.Should().Be("Host=remote;Database=birds;Username=user;Password=secret");
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

        private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }
    }
}
