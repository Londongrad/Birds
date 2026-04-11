using System.Collections.ObjectModel;
using Birds.Shared.Sync;
using Birds.UI.Threading.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Sync;

public sealed class RemoteSyncStatusStore : ObservableObject, IRemoteSyncStatusSource, IRemoteSyncStatusReporter
{
    private const int MaxRecentActivityEntries = 6;
    private readonly ObservableCollection<RemoteSyncActivityEntry> _recentActivity = [];
    private readonly ReadOnlyObservableCollection<RemoteSyncActivityEntry> _recentActivityView;

    private readonly IUiDispatcher _uiDispatcher;
    private DateTime? _lastAttemptAtUtc;
    private string? _lastErrorMessage;
    private int _lastProcessedCount;
    private DateTime? _lastSuccessfulSyncAtUtc;
    private int _pendingOperationCount;

    private RemoteSyncDisplayState _status = RemoteSyncDisplayState.Disabled;

    public RemoteSyncStatusStore(IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher;
        _recentActivityView = new ReadOnlyObservableCollection<RemoteSyncActivityEntry>(_recentActivity);
    }

    public Task SetDisabledAsync(int pendingOperationCount, CancellationToken cancellationToken = default)
    {
        return _uiDispatcher.InvokeAsync(() =>
        {
            Status = RemoteSyncDisplayState.Disabled;
            LastAttemptAtUtc = null;
            LastSuccessfulSyncAtUtc = null;
            LastErrorMessage = null;
            LastProcessedCount = 0;
            PendingOperationCount = pendingOperationCount;
            _recentActivity.Clear();
            OnPropertyChanged(nameof(RecentActivity));
        }, cancellationToken);
    }

    public Task SetPausedAsync(int pendingOperationCount, CancellationToken cancellationToken = default)
    {
        return _uiDispatcher.InvokeAsync(() =>
        {
            Status = RemoteSyncDisplayState.Paused;
            LastErrorMessage = null;
            PendingOperationCount = pendingOperationCount;
            TryAppendActivity(RemoteSyncDisplayState.Paused, DateTime.UtcNow, 0, pendingOperationCount);
        }, cancellationToken);
    }

    public Task SetSyncingAsync(int pendingOperationCount, CancellationToken cancellationToken = default)
    {
        return _uiDispatcher.InvokeAsync(() =>
        {
            Status = RemoteSyncDisplayState.Syncing;
            LastAttemptAtUtc = DateTime.UtcNow;
            LastErrorMessage = null;
            PendingOperationCount = pendingOperationCount;
        }, cancellationToken);
    }

    public Task SetResultAsync(RemoteSyncDisplayState status,
        int processedCount,
        int pendingOperationCount,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        return _uiDispatcher.InvokeAsync(() =>
        {
            var attemptUtc = DateTime.UtcNow;

            Status = status;
            LastAttemptAtUtc = attemptUtc;
            LastProcessedCount = processedCount;
            LastErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage;
            PendingOperationCount = pendingOperationCount;

            if (status == RemoteSyncDisplayState.Synced)
                LastSuccessfulSyncAtUtc = attemptUtc;
            else if (status == RemoteSyncDisplayState.Disabled)
                LastSuccessfulSyncAtUtc = null;

            TryAppendActivity(status, attemptUtc, processedCount, pendingOperationCount, LastErrorMessage);
        }, cancellationToken);
    }

    public Task SetLoopFailedAsync(string errorMessage,
        int pendingOperationCount,
        CancellationToken cancellationToken = default)
    {
        return _uiDispatcher.InvokeAsync(() =>
        {
            Status = RemoteSyncDisplayState.Error;
            LastAttemptAtUtc = DateTime.UtcNow;
            LastProcessedCount = 0;
            PendingOperationCount = pendingOperationCount;
            LastErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage;
            TryAppendActivity(RemoteSyncDisplayState.Error, LastAttemptAtUtc.Value, 0, pendingOperationCount,
                LastErrorMessage);
        }, cancellationToken);
    }

    public RemoteSyncDisplayState Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public DateTime? LastSuccessfulSyncAtUtc
    {
        get => _lastSuccessfulSyncAtUtc;
        private set => SetProperty(ref _lastSuccessfulSyncAtUtc, value);
    }

    public DateTime? LastAttemptAtUtc
    {
        get => _lastAttemptAtUtc;
        private set => SetProperty(ref _lastAttemptAtUtc, value);
    }

    public string? LastErrorMessage
    {
        get => _lastErrorMessage;
        private set => SetProperty(ref _lastErrorMessage, value);
    }

    public int LastProcessedCount
    {
        get => _lastProcessedCount;
        private set => SetProperty(ref _lastProcessedCount, value);
    }

    public int PendingOperationCount
    {
        get => _pendingOperationCount;
        private set => SetProperty(ref _pendingOperationCount, value);
    }

    public IReadOnlyList<RemoteSyncActivityEntry> RecentActivity => _recentActivityView;

    private void TryAppendActivity(RemoteSyncDisplayState status,
        DateTime occurredAtUtc,
        int processedCount,
        int pendingOperationCount,
        string? errorMessage = null)
    {
        if (status is RemoteSyncDisplayState.Disabled or RemoteSyncDisplayState.Syncing)
            return;

        if (_recentActivity.Count > 0)
        {
            var latest = _recentActivity[0];
            var isSameEntry = latest.Status == status
                              && latest.ProcessedCount == processedCount
                              && latest.PendingOperationCount == pendingOperationCount
                              && string.Equals(latest.ErrorMessage, errorMessage, StringComparison.Ordinal);

            if (isSameEntry)
                return;

            if (status == RemoteSyncDisplayState.Synced
                && processedCount == 0
                && latest.Status == RemoteSyncDisplayState.Synced
                && latest.ProcessedCount == 0)
                return;
        }

        _recentActivity.Insert(0,
            new RemoteSyncActivityEntry(status, occurredAtUtc, processedCount, pendingOperationCount, errorMessage));

        while (_recentActivity.Count > MaxRecentActivityEntries)
            _recentActivity.RemoveAt(_recentActivity.Count - 1);

        OnPropertyChanged(nameof(RecentActivity));
    }
}