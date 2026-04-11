namespace Birds.Infrastructure.Services;

public enum RemoteSyncRunStatus
{
    Disabled = 0,
    NothingToSync = 1,
    Synced = 2,
    BackendUnavailable = 3,
    Failed = 4
}