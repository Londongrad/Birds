using System.ComponentModel;

namespace Birds.Shared.Sync;

public interface IRemoteSyncStatusSource : INotifyPropertyChanged
{
    RemoteSyncDisplayState Status { get; }

    DateTime? LastSuccessfulSyncAtUtc { get; }

    DateTime? LastAttemptAtUtc { get; }

    string? LastErrorMessage { get; }

    int LastProcessedCount { get; }

    int PendingOperationCount { get; }

    IReadOnlyList<RemoteSyncActivityEntry> RecentActivity { get; }
}