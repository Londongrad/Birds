namespace Birds.Infrastructure.Persistence.Models;

public sealed class RemoteSyncCursor
{
    private RemoteSyncCursor()
    {
    }

    public string CursorKey { get; private set; } = string.Empty;
    public DateTime? LastSyncedAtUtc { get; private set; }

    public static RemoteSyncCursor Create(string cursorKey, DateTime? lastSyncedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cursorKey);

        return new RemoteSyncCursor
        {
            CursorKey = cursorKey,
            LastSyncedAtUtc = lastSyncedAtUtc
        };
    }

    public void AdvanceTo(DateTime syncedAtUtc)
    {
        if (LastSyncedAtUtc is null || syncedAtUtc > LastSyncedAtUtc.Value)
            LastSyncedAtUtc = syncedAtUtc;
    }
}