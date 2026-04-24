using Birds.Infrastructure;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WhenRemoteSyncIsDisabled_Should_NotRegisterRemotePostgresContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(
            $"Data Source={Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")}",
            new DatabaseSeedingOptions(DatabaseSeedingMode.None, 0, 1, 0),
            RemoteSyncRuntimeOptions.Disabled);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRemoteSyncService>().Should().BeOfType<DisabledRemoteSyncService>();
        provider.GetService<IRemoteSyncSchemaInitializer>().Should().BeNull();
        provider.GetService<IDbContextFactory<RemoteBirdDbContext>>().Should().BeNull();
    }

    [Fact]
    public void AddInfrastructure_WhenRemoteSyncIsMisconfigured_Should_NotRegisterRemotePostgresContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(
            $"Data Source={Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")}",
            new DatabaseSeedingOptions(DatabaseSeedingMode.None, 0, 1, 0),
            RemoteSyncRuntimeOptions.EnabledButNotConfigured("missing configuration"));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRemoteSyncService>().Should().BeOfType<DisabledRemoteSyncService>();
        provider.GetService<IRemoteSyncSchemaInitializer>().Should().BeNull();
        provider.GetService<IDbContextFactory<RemoteBirdDbContext>>().Should().BeNull();
    }

    [Fact]
    public void AddInfrastructure_WhenRemoteSyncIsConfigured_Should_RegisterRemoteSchemaInitializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(
            $"Data Source={Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")}",
            new DatabaseSeedingOptions(DatabaseSeedingMode.None, 0, 1, 0),
            new RemoteSyncRuntimeOptions(
                true,
                "Host=localhost;Database=birds;Username=user;Password=secret"));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRemoteSyncSchemaInitializer>()
            .Should()
            .BeOfType<RemoteSyncSchemaInitializer>();
        provider.GetRequiredService<IRemoteSyncService>().Should().BeOfType<RemoteSyncService>();
    }
}
