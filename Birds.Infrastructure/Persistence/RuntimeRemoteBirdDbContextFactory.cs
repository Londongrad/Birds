using Birds.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Persistence;

public sealed class RuntimeRemoteBirdDbContextFactory(
    IRemoteSyncRuntimeOptionsProvider remoteSyncOptionsProvider) : IDbContextFactory<RemoteBirdDbContext>
{
    private readonly IRemoteSyncRuntimeOptionsProvider _remoteSyncOptionsProvider = remoteSyncOptionsProvider;

    public RemoteBirdDbContext CreateDbContext()
    {
        var options = _remoteSyncOptionsProvider.Current;
        if (!options.IsConfigured)
            throw new InvalidOperationException(
                options.ConfigurationErrorMessage ?? "Remote PostgreSQL synchronization is not configured.");

        var builder = new DbContextOptionsBuilder<RemoteBirdDbContext>();
        builder.UseNpgsql(options.ConnectionString!, npgsql => npgsql.EnableRetryOnFailure(0));

        return new RemoteBirdDbContext(builder.Options);
    }
}
