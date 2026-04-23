namespace Birds.Infrastructure.Persistence.Models;

public sealed class RemoteSyncCursor
{
    private RemoteSyncCursor()
    {
    }

    public string CursorKey { get; private set; } = string.Empty;
    public DateTime? LastSyncedAtUtc { get; private set; }
    public Guid? LastSyncedEntityId { get; private set; }

    public static RemoteSyncCursor Create(string cursorKey,
        DateTime? lastSyncedAtUtc = null,
        Guid? lastSyncedEntityId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cursorKey);

        return new RemoteSyncCursor
        {
            CursorKey = cursorKey,
            LastSyncedAtUtc = lastSyncedAtUtc.HasValue
                ? UtcDateTimeStorage.Normalize(lastSyncedAtUtc.Value)
                : null,
            LastSyncedEntityId = lastSyncedAtUtc.HasValue ? lastSyncedEntityId : null
        };
    }

    public void AdvanceTo(DateTime syncedAtUtc, Guid syncedEntityId)
    {
        var normalized = UtcDateTimeStorage.Normalize(syncedAtUtc);
        var shouldAdvance = LastSyncedAtUtc is null ||
                            normalized > LastSyncedAtUtc.Value ||
                            normalized == LastSyncedAtUtc.Value &&
                            (LastSyncedEntityId is null || syncedEntityId.CompareTo(LastSyncedEntityId.Value) > 0);

        if (shouldAdvance)
        {
            LastSyncedAtUtc = normalized;
            LastSyncedEntityId = syncedEntityId;
        }
    }
}
