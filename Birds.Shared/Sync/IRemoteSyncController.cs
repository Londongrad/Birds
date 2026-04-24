namespace Birds.Shared.Sync;

public interface IRemoteSyncController
{
    bool IsEnabled { get; }

    bool IsConfigured { get; }

    string? ConfigurationErrorMessage { get; }

    Task SyncNowAsync(CancellationToken cancellationToken);

    Task<bool> RedownloadRemoteSnapshotAsync(CancellationToken cancellationToken);

    Task<bool> UploadLocalSnapshotToRemoteAsync(CancellationToken cancellationToken);

    Task PauseAsync(CancellationToken cancellationToken);

    Task ResumeAsync(CancellationToken cancellationToken);
}
