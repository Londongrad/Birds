using Birds.Infrastructure.Persistence;

namespace Birds.Infrastructure.Services;

public interface IRemoteSyncSchemaInitializer
{
    Task InitializeAsync(RemoteBirdDbContext remoteContext, CancellationToken cancellationToken);
}
