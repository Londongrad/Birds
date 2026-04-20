namespace Birds.Shared.Sync;

public interface IRemoteSyncStatusReporter
{
    Task SetDisabledAsync(int pendingOperationCount, CancellationToken cancellationToken = default);

    Task SetRemoteSnapshotStateAsync(RemoteSyncSnapshotState snapshotState,
        int? remoteBirdCount = null,
        CancellationToken cancellationToken = default);

    Task SetPausedAsync(int pendingOperationCount, CancellationToken cancellationToken = default);

    Task SetSyncingAsync(int pendingOperationCount, CancellationToken cancellationToken = default);

    Task SetResultAsync(RemoteSyncDisplayState status,
        int processedCount,
        int pendingOperationCount,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    Task SetLoopFailedAsync(string errorMessage,
        int pendingOperationCount,
        CancellationToken cancellationToken = default);
}
