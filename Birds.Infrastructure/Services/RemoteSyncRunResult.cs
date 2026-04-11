namespace Birds.Infrastructure.Services;

public sealed record RemoteSyncRunResult(
    RemoteSyncRunStatus Status,
    int ProcessedCount,
    string? ErrorMessage = null,
    int RemoteWinsCount = 0)
{
    public static RemoteSyncRunResult Disabled { get; } = new(RemoteSyncRunStatus.Disabled, 0);
    public static RemoteSyncRunResult NothingToSync { get; } = new(RemoteSyncRunStatus.NothingToSync, 0);
}