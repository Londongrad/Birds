namespace Birds.Shared.Sync;

public interface IRemoteSyncController
{
    bool IsConfigured { get; }

    Task SyncNowAsync(CancellationToken cancellationToken);

    Task<bool> RedownloadRemoteSnapshotAsync(CancellationToken cancellationToken);

    Task<bool> UploadLocalSnapshotToRemoteAsync(CancellationToken cancellationToken);

    Task PauseAsync(CancellationToken cancellationToken);

    Task ResumeAsync(CancellationToken cancellationToken);
}
