namespace Birds.Infrastructure.Services
{
    public sealed class DisabledRemoteSyncService : IRemoteSyncService
    {
        public Task<RemoteSyncRunResult> SyncPendingAsync(CancellationToken cancellationToken)
            => Task.FromResult(RemoteSyncRunResult.Disabled);
    }
}
