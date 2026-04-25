namespace Birds.Infrastructure.Configuration;

public interface IRemoteSyncRuntimeOptionsProvider
{
    event EventHandler? Changed;

    RemoteSyncRuntimeOptions Current { get; }

    void Refresh();
}
