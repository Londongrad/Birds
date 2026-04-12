namespace Birds.Infrastructure.Services;

public sealed class DisabledRemoteSyncService : IRemoteSyncService
{
    public Task<RemoteSyncBackendCheckResult> CheckBackendAvailabilityAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Disabled));
    }

    public Task<RemoteSyncRunResult> SyncPendingAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(RemoteSyncRunResult.Disabled);
    }
}