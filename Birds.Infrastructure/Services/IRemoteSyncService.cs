namespace Birds.Infrastructure.Services;

public interface IRemoteSyncService
{
    Task<RemoteSyncRunResult> SyncPendingAsync(CancellationToken cancellationToken);
}