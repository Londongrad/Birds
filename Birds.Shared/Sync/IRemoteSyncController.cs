namespace Birds.Shared.Sync
{
    public interface IRemoteSyncController
    {
        bool IsConfigured { get; }

        Task SyncNowAsync(CancellationToken cancellationToken);

        Task PauseAsync(CancellationToken cancellationToken);

        Task ResumeAsync(CancellationToken cancellationToken);
    }
}
