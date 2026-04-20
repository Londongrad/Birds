using Birds.Shared.Sync;

namespace Birds.App.Services;

public interface IRemoteSyncCoordinator : IRemoteSyncController
{
    void Start(CancellationToken stoppingToken);

    Task BootstrapLocalStoreAsync(CancellationToken cancellationToken);
}
