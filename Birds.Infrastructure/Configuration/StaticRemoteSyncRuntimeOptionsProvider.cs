namespace Birds.Infrastructure.Configuration;

public sealed class StaticRemoteSyncRuntimeOptionsProvider(RemoteSyncRuntimeOptions options)
    : IRemoteSyncRuntimeOptionsProvider
{
    public event EventHandler? Changed;

    public RemoteSyncRuntimeOptions Current { get; } = options;

    public void Refresh()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
