using Birds.Shared.Sync;

namespace Birds.Infrastructure.Services;

public sealed record RemoteSyncBackendCheckResult(
    RemoteSyncRunStatus Status,
    string? ErrorMessage = null,
    int? RemoteBirdCount = null)
{
    public static RemoteSyncBackendCheckResult Ready { get; } = new(RemoteSyncRunStatus.Synced);

    public bool IsReady => Status == RemoteSyncRunStatus.Synced;

    public RemoteSyncSnapshotState RemoteSnapshotState => !IsReady
        ? RemoteSyncSnapshotState.Unknown
        : RemoteBirdCount switch
        {
            0 => RemoteSyncSnapshotState.Empty,
            > 0 => RemoteSyncSnapshotState.HasData,
            _ => RemoteSyncSnapshotState.Unknown
        };
}
