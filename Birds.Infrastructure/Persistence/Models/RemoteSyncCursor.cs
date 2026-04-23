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
            LastSyncedAtUtc = lastSyncedAtUtc.HasValue
                ? UtcDateTimeStorage.Normalize(lastSyncedAtUtc.Value)
                : null
        };
    }

    public void AdvanceTo(DateTime syncedAtUtc)
    {
        var normalized = UtcDateTimeStorage.Normalize(syncedAtUtc);
        if (LastSyncedAtUtc is null || normalized > LastSyncedAtUtc.Value)
            LastSyncedAtUtc = normalized;
    }
}
