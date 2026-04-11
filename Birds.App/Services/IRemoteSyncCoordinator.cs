namespace Birds.App.Services
{
    public interface IRemoteSyncCoordinator
    {
        void Start(CancellationToken stoppingToken);

        Task BootstrapLocalStoreAsync(CancellationToken cancellationToken);
    }
}
