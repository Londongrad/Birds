namespace Birds.Infrastructure.Services;

public sealed class DisabledRemoteSyncService : IRemoteSyncService
{
    public Task<RemoteSyncRunResult> SyncPendingAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(RemoteSyncRunResult.Disabled);
    }
}