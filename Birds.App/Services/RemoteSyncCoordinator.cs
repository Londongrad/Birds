using Birds.Application.Interfaces;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Services;
using Birds.Shared.Constants;
using Birds.Shared.Sync;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Notification.Interfaces;
using Serilog;

namespace Birds.App.Services;

internal sealed class RemoteSyncCoordinator(
    IRemoteSyncService remoteSyncService,
    RemoteSyncRuntimeOptions remoteSyncOptions,
    IRemoteSyncStatusReporter remoteSyncStatusReporter,
    ILocalStoreStateService localStoreStateService,
    IDatabaseMaintenanceService databaseMaintenanceService,
    IAppPreferencesService preferences,
    INotificationService notificationService) : IRemoteSyncCoordinator
{
    private const int MaxBootstrapPasses = 512;
    private readonly IDatabaseMaintenanceService _databaseMaintenanceService = databaseMaintenanceService;
    private readonly ILocalStoreStateService _localStoreStateService = localStoreStateService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppPreferencesService _preferences = preferences;
    private readonly RemoteSyncRuntimeOptions _remoteSyncOptions = remoteSyncOptions;

    private readonly IRemoteSyncService _remoteSyncService = remoteSyncService;
    private readonly IRemoteSyncStatusReporter _remoteSyncStatusReporter = remoteSyncStatusReporter;
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private readonly SemaphoreSlim _wakeSignal = new(0, int.MaxValue);
    private volatile bool _isPaused;
    private int _started;

    public bool IsConfigured => _remoteSyncOptions.IsConfigured;

    public void Start(CancellationToken stoppingToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            _ = PublishDisabledStateAsync(CancellationToken.None);
            return;
        }

        if (Interlocked.Exchange(ref _started, 1) == 1)
            return;

        _ = Task.Run(() => RunAsync(stoppingToken), CancellationToken.None);
    }

    public async Task BootstrapLocalStoreAsync(CancellationToken cancellationToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            await PublishDisabledStateAsync(cancellationToken);
            return;
        }

        await _runLock.WaitAsync(cancellationToken);
        try
        {
            await BootstrapLocalStoreCoreAsync(cancellationToken);
        }
        finally
        {
            _runLock.Release();
        }
    }

    public async Task<bool> RedownloadRemoteSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            await PublishDisabledStateAsync(cancellationToken);
            return false;
        }

        await _runLock.WaitAsync(cancellationToken);
        try
        {
            var wasPaused = _isPaused;
            await PublishSyncingStateAsync(cancellationToken);

            var backendCheck = await _remoteSyncService.CheckBackendAvailabilityAsync(cancellationToken);
            if (!backendCheck.IsReady)
            {
                var currentState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
                await _remoteSyncStatusReporter.SetResultAsync(
                    ToDisplayState(backendCheck.Status),
                    0,
                    currentState.PendingOperationCount,
                    backendCheck.ErrorMessage,
                    cancellationToken);
                await SetRemoteSnapshotStateSafeAsync(
                    RemoteSyncSnapshotState.Unknown,
                    null,
                    cancellationToken);
                return false;
            }

            await _databaseMaintenanceService.ResetLocalDatabaseAsync(cancellationToken);
            var bootstrapSucceeded = await BootstrapLocalStoreCoreAsync(cancellationToken);

            if (wasPaused && bootstrapSucceeded)
            {
                var localState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
                await _remoteSyncStatusReporter.SetPausedAsync(localState.PendingOperationCount, cancellationToken);
            }

            return bootstrapSucceeded;
        }
        finally
        {
            _runLock.Release();
        }
    }

    public async Task<bool> UploadLocalSnapshotToRemoteAsync(CancellationToken cancellationToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            await PublishDisabledStateAsync(cancellationToken);
            return false;
        }

        await _runLock.WaitAsync(cancellationToken);
        try
        {
            var wasPaused = _isPaused;
            await PublishSyncingStateAsync(cancellationToken);

            var result = await _remoteSyncService.UploadLocalSnapshotAsync(cancellationToken);
            var localState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
            await _remoteSyncStatusReporter.SetResultAsync(
                ToDisplayState(result.Status),
                result.ProcessedCount,
                localState.PendingOperationCount,
                result.ErrorMessage,
                cancellationToken);
            await PublishRemoteSnapshotStateAsync(result.Status, cancellationToken);

            if (wasPaused && result.Status == RemoteSyncRunStatus.Synced)
                await _remoteSyncStatusReporter.SetPausedAsync(localState.PendingOperationCount, cancellationToken);

            return result.Status == RemoteSyncRunStatus.Synced;
        }
        finally
        {
            _runLock.Release();
        }
    }

    public async Task SyncNowAsync(CancellationToken cancellationToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            await PublishDisabledStateAsync(cancellationToken);
            return;
        }

        await ExecuteSyncIterationAsync(cancellationToken);

        if (_isPaused)
        {
            var localState = await TryGetLocalStateAsync(cancellationToken);
            await _remoteSyncStatusReporter.SetPausedAsync(localState.PendingOperationCount, cancellationToken);
        }
    }

    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            await PublishDisabledStateAsync(cancellationToken);
            return;
        }

        _isPaused = true;
        var localState = await TryGetLocalStateAsync(cancellationToken);
        await _remoteSyncStatusReporter.SetPausedAsync(localState.PendingOperationCount, cancellationToken);
    }

    public Task ResumeAsync(CancellationToken cancellationToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
            return PublishDisabledStateAsync(cancellationToken);

        _isPaused = false;
        RequestWake();
        return Task.CompletedTask;
    }

    private async Task<bool> BootstrapLocalStoreCoreAsync(CancellationToken cancellationToken)
    {
        await PublishSyncingStateAsync(cancellationToken);
        var totalProcessed = 0;
        var totalRemoteWins = 0;

        for (var pass = 0; pass < MaxBootstrapPasses; pass++)
        {
            RemoteSyncRunResult result;
            try
            {
                result = await _remoteSyncService.SyncPendingAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var localState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
                await _remoteSyncStatusReporter.SetResultAsync(
                    RemoteSyncDisplayState.Error,
                    totalProcessed,
                    localState.PendingOperationCount,
                    ex.Message,
                    cancellationToken);
                Log.Error(ex, LogMessages.RemoteSyncLoopFailed);
                return false;
            }

            totalProcessed += result.ProcessedCount;
            totalRemoteWins += result.RemoteWinsCount;

            switch (result.Status)
            {
                case RemoteSyncRunStatus.Synced:
                    continue;

                case RemoteSyncRunStatus.NothingToSync:
                    var syncedState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
                    await _remoteSyncStatusReporter.SetResultAsync(
                        RemoteSyncDisplayState.Synced,
                        totalProcessed,
                        syncedState.PendingOperationCount,
                        null,
                        cancellationToken);
                    await PublishRemoteSnapshotStateAsync(RemoteSyncRunStatus.NothingToSync, cancellationToken);
                    ShowConflictResolutionWarning(totalRemoteWins);
                    return true;

                default:
                    var localState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
                    await _remoteSyncStatusReporter.SetResultAsync(
                        ToDisplayState(result.Status),
                        totalProcessed,
                        localState.PendingOperationCount,
                        result.ErrorMessage,
                        cancellationToken);
                    await PublishRemoteSnapshotStateAsync(result.Status, cancellationToken);
                    ShowConflictResolutionWarning(totalRemoteWins);
                    return false;
            }
        }

        const string bootstrapExceededMessage = "Remote bootstrap synchronization exceeded the maximum batch limit.";
        var finalState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
        await _remoteSyncStatusReporter.SetResultAsync(
            RemoteSyncDisplayState.Error,
            totalProcessed,
            finalState.PendingOperationCount,
            bootstrapExceededMessage,
            cancellationToken);
        await SetRemoteSnapshotStateSafeAsync(
            RemoteSyncSnapshotState.Unknown,
            null,
            cancellationToken);
        ShowConflictResolutionWarning(totalRemoteWins);
        Log.Warning(bootstrapExceededMessage);
        return false;
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(4);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_isPaused)
                {
                    var localState = await _localStoreStateService.GetSnapshotAsync(stoppingToken);
                    await _remoteSyncStatusReporter.SetPausedAsync(localState.PendingOperationCount, stoppingToken);
                    await WaitForNextTriggerAsync(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                }

                delay = await RunSingleIterationAsync(stoppingToken);
                await WaitForNextTriggerAsync(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Log.Information(LogMessages.RemoteSyncStopped);
        }
    }

    internal async Task<TimeSpan> RunSingleIterationAsync(CancellationToken stoppingToken)
    {
        if (!_remoteSyncOptions.IsConfigured)
        {
            await PublishDisabledStateAsync(stoppingToken);
            return TimeSpan.FromSeconds(15);
        }

        var result = await ExecuteSyncIterationAsync(stoppingToken);

        return result.Status switch
        {
            RemoteSyncRunStatus.Synced => TimeSpan.FromSeconds(3),
            RemoteSyncRunStatus.NothingToSync => RemoteSyncIntervalPresets.ToTimeSpan(_preferences.SelectedSyncInterval),
            RemoteSyncRunStatus.BackendUnavailable => TimeSpan.FromSeconds(20),
            RemoteSyncRunStatus.Failed => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(15)
        };
    }

    private static RemoteSyncDisplayState ToDisplayState(RemoteSyncRunStatus status)
    {
        return status switch
        {
            RemoteSyncRunStatus.Synced => RemoteSyncDisplayState.Synced,
            RemoteSyncRunStatus.NothingToSync => RemoteSyncDisplayState.Synced,
            RemoteSyncRunStatus.BackendUnavailable => RemoteSyncDisplayState.Offline,
            RemoteSyncRunStatus.Failed => RemoteSyncDisplayState.Error,
            _ => RemoteSyncDisplayState.Disabled
        };
    }

    private async Task<RemoteSyncRunResult> ExecuteSyncIterationAsync(CancellationToken cancellationToken)
    {
        await _runLock.WaitAsync(cancellationToken);
        try
        {
            await PublishSyncingStateAsync(cancellationToken);

            var result = await _remoteSyncService.SyncPendingAsync(cancellationToken);
            var localState = await _localStoreStateService.GetSnapshotAsync(cancellationToken);
            await _remoteSyncStatusReporter.SetResultAsync(
                ToDisplayState(result.Status),
                result.ProcessedCount,
                localState.PendingOperationCount,
                result.ErrorMessage,
                cancellationToken);
            await PublishRemoteSnapshotStateAsync(result.Status, cancellationToken);
            ShowConflictResolutionWarning(result.RemoteWinsCount);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var pendingCount = await TryGetPendingOperationCountAsync(cancellationToken);
            await _remoteSyncStatusReporter.SetLoopFailedAsync(ex.Message, pendingCount, CancellationToken.None);
            await SetRemoteSnapshotStateSafeAsync(
                RemoteSyncSnapshotState.Unknown,
                null,
                CancellationToken.None);
            Log.Error(ex, LogMessages.RemoteSyncLoopFailed);
            return new RemoteSyncRunResult(RemoteSyncRunStatus.Failed, 0, ex.Message);
        }
        finally
        {
            _runLock.Release();
        }
    }

    private async Task PublishDisabledStateAsync(CancellationToken cancellationToken)
    {
        var localState = await TryGetLocalStateAsync(cancellationToken);
        await _remoteSyncStatusReporter.SetDisabledAsync(localState.PendingOperationCount, cancellationToken);
    }

    private async Task PublishSyncingStateAsync(CancellationToken cancellationToken)
    {
        var localState = await TryGetLocalStateAsync(cancellationToken);
        await _remoteSyncStatusReporter.SetSyncingAsync(localState.PendingOperationCount, cancellationToken);
    }

    private async Task<LocalStoreStateSnapshot> TryGetLocalStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _localStoreStateService.GetSnapshotAsync(cancellationToken);
        }
        catch
        {
            return new LocalStoreStateSnapshot(0, 0);
        }
    }

    private async Task<int> TryGetPendingOperationCountAsync(CancellationToken cancellationToken)
    {
        var state = await TryGetLocalStateAsync(cancellationToken);
        return state.PendingOperationCount;
    }

    private async Task PublishRemoteSnapshotStateAsync(RemoteSyncRunStatus syncStatus,
        CancellationToken cancellationToken)
    {
        if (syncStatus is not (RemoteSyncRunStatus.Synced or RemoteSyncRunStatus.NothingToSync))
        {
            await SetRemoteSnapshotStateSafeAsync(
                RemoteSyncSnapshotState.Unknown,
                null,
                cancellationToken);
            return;
        }

        var backendCheckTask = _remoteSyncService.CheckBackendAvailabilityAsync(cancellationToken);
        var backendCheck = backendCheckTask is null
            ? new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Failed)
            : await backendCheckTask ?? new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Failed);
        if (!backendCheck.IsReady)
        {
            await SetRemoteSnapshotStateSafeAsync(
                RemoteSyncSnapshotState.Unknown,
                null,
                cancellationToken);
            return;
        }

        await SetRemoteSnapshotStateSafeAsync(
            backendCheck.RemoteSnapshotState,
            backendCheck.RemoteBirdCount,
            cancellationToken);
    }

    private Task SetRemoteSnapshotStateSafeAsync(RemoteSyncSnapshotState snapshotState,
        int? remoteBirdCount,
        CancellationToken cancellationToken)
    {
        return _remoteSyncStatusReporter.SetRemoteSnapshotStateAsync(snapshotState, remoteBirdCount, cancellationToken)
               ?? Task.CompletedTask;
    }

    private async Task WaitForNextTriggerAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        using var wakeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var wakeTask = _wakeSignal.WaitAsync(wakeCancellation.Token);
        var delayTask = Task.Delay(delay, delayCancellation.Token);
        var completedTask = await Task.WhenAny(delayTask, wakeTask);

        if (completedTask == delayTask)
        {
            wakeCancellation.Cancel();

            try
            {
                await wakeTask;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Delay completed first; cancel the pending wake waiter so it does not consume a future signal.
            }
        }
        else
        {
            delayCancellation.Cancel();

            try
            {
                await delayTask;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Wake signal completed first; cancel the pending delay so it does not keep running in the background.
            }
        }
    }

    private void RequestWake()
    {
        try
        {
            _wakeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake request is already queued.
        }
    }

    private void ShowConflictResolutionWarning(int remoteWinsCount)
    {
        if (remoteWinsCount <= 0)
            return;

        _notificationService.ShowWarningLocalized("Info.SyncConflictResolved", remoteWinsCount);
    }
}
