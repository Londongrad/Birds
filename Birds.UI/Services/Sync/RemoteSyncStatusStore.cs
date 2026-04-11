using Birds.Shared.Sync;
using Birds.UI.Threading.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Sync
{
    public sealed class RemoteSyncStatusStore(IUiDispatcher uiDispatcher)
        : ObservableObject, IRemoteSyncStatusSource, IRemoteSyncStatusReporter
    {
        private readonly IUiDispatcher _uiDispatcher = uiDispatcher;

        private RemoteSyncDisplayState _status = RemoteSyncDisplayState.Disabled;
        private DateTime? _lastSuccessfulSyncAtUtc;
        private DateTime? _lastAttemptAtUtc;
        private string? _lastErrorMessage;
        private int _lastProcessedCount;
        private int _pendingOperationCount;

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

        public Task SetDisabledAsync(int pendingOperationCount, CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Disabled;
                LastAttemptAtUtc = null;
                LastSuccessfulSyncAtUtc = null;
                LastErrorMessage = null;
                LastProcessedCount = 0;
                PendingOperationCount = pendingOperationCount;
            }, cancellationToken);

        public Task SetPausedAsync(int pendingOperationCount, CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Paused;
                LastErrorMessage = null;
                PendingOperationCount = pendingOperationCount;
            }, cancellationToken);

        public Task SetSyncingAsync(int pendingOperationCount, CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Syncing;
                LastAttemptAtUtc = DateTime.UtcNow;
                LastErrorMessage = null;
                PendingOperationCount = pendingOperationCount;
            }, cancellationToken);

        public Task SetResultAsync(RemoteSyncDisplayState status,
                                   int processedCount,
                                   int pendingOperationCount,
                                   string? errorMessage = null,
                                   CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
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
            }, cancellationToken);

        public Task SetLoopFailedAsync(string errorMessage,
                                       int pendingOperationCount,
                                       CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Error;
                LastAttemptAtUtc = DateTime.UtcNow;
                LastProcessedCount = 0;
                PendingOperationCount = pendingOperationCount;
                LastErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? null
                    : errorMessage;
            }, cancellationToken);
    }
}
