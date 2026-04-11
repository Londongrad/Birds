namespace Birds.Shared.Sync
{
    public interface IRemoteSyncStatusReporter
    {
        Task SetDisabledAsync(CancellationToken cancellationToken = default);

        Task SetSyncingAsync(CancellationToken cancellationToken = default);

        Task SetResultAsync(RemoteSyncDisplayState status,
                            int processedCount,
                            string? errorMessage = null,
                            CancellationToken cancellationToken = default);

        Task SetLoopFailedAsync(string errorMessage, CancellationToken cancellationToken = default);
    }
}
