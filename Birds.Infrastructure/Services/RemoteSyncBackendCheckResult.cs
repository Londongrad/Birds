namespace Birds.Infrastructure.Services;

public sealed record RemoteSyncBackendCheckResult(RemoteSyncRunStatus Status, string? ErrorMessage = null)
{
    public static RemoteSyncBackendCheckResult Ready { get; } = new(RemoteSyncRunStatus.Synced);

    public bool IsReady => Status == RemoteSyncRunStatus.Synced;
}