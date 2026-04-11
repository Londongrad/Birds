namespace Birds.Shared.Sync
{
    public sealed record RemoteSyncActivityEntry(
        RemoteSyncDisplayState Status,
        DateTime OccurredAtUtc,
        int ProcessedCount,
        int PendingOperationCount,
        string? ErrorMessage = null);
}
