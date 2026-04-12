namespace Birds.Infrastructure.Services;

public interface IRemoteSyncService
{
    Task<RemoteSyncBackendCheckResult> CheckBackendAvailabilityAsync(CancellationToken cancellationToken);

    Task<RemoteSyncRunResult> SyncPendingAsync(CancellationToken cancellationToken);
}