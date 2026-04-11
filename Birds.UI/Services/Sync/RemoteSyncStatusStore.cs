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

        public Task SetDisabledAsync(CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Disabled;
                LastAttemptAtUtc = null;
                LastSuccessfulSyncAtUtc = null;
                LastErrorMessage = null;
                LastProcessedCount = 0;
            }, cancellationToken);

        public Task SetSyncingAsync(CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Syncing;
                LastAttemptAtUtc = DateTime.UtcNow;
                LastErrorMessage = null;
            }, cancellationToken);

        public Task SetResultAsync(RemoteSyncDisplayState status,
                                   int processedCount,
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

                if (status == RemoteSyncDisplayState.Synced)
                    LastSuccessfulSyncAtUtc = attemptUtc;
                else if (status == RemoteSyncDisplayState.Disabled)
                    LastSuccessfulSyncAtUtc = null;
            }, cancellationToken);

        public Task SetLoopFailedAsync(string errorMessage, CancellationToken cancellationToken = default)
            => _uiDispatcher.InvokeAsync(() =>
            {
                Status = RemoteSyncDisplayState.Error;
                LastAttemptAtUtc = DateTime.UtcNow;
                LastProcessedCount = 0;
                LastErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? null
                    : errorMessage;
            }, cancellationToken);
    }
}
