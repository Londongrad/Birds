using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Services;
using Birds.Shared.Sync;
using Birds.Shared.Constants;
using Serilog;

namespace Birds.App.Services
{
    internal sealed class RemoteSyncCoordinator(
        IRemoteSyncService remoteSyncService,
        RemoteSyncRuntimeOptions remoteSyncOptions,
        IRemoteSyncStatusReporter remoteSyncStatusReporter) : IRemoteSyncCoordinator
    {
        private const int MaxBootstrapPasses = 512;

        private readonly IRemoteSyncService _remoteSyncService = remoteSyncService;
        private readonly RemoteSyncRuntimeOptions _remoteSyncOptions = remoteSyncOptions;
        private readonly IRemoteSyncStatusReporter _remoteSyncStatusReporter = remoteSyncStatusReporter;
        private int _started;

        public void Start(CancellationToken stoppingToken)
        {
            if (!_remoteSyncOptions.IsConfigured)
            {
                _ = _remoteSyncStatusReporter.SetDisabledAsync(CancellationToken.None);
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
                await _remoteSyncStatusReporter.SetDisabledAsync(cancellationToken);
                return;
            }

            await _remoteSyncStatusReporter.SetSyncingAsync(cancellationToken);

            var totalProcessed = 0;

            for (var pass = 0; pass < MaxBootstrapPasses; pass++)
            {
                var result = await _remoteSyncService.SyncPendingAsync(cancellationToken);
                totalProcessed += result.ProcessedCount;

                switch (result.Status)
                {
                    case RemoteSyncRunStatus.Synced:
                        continue;

                    case RemoteSyncRunStatus.NothingToSync:
                        await _remoteSyncStatusReporter.SetResultAsync(
                            RemoteSyncDisplayState.Synced,
                            totalProcessed,
                            null,
                            cancellationToken);
                        return;

                    default:
                        await _remoteSyncStatusReporter.SetResultAsync(
                            ToDisplayState(result.Status),
                            totalProcessed,
                            result.ErrorMessage,
                            cancellationToken);
                        return;
                }
            }

            const string bootstrapExceededMessage = "Remote bootstrap synchronization exceeded the maximum batch limit.";
            await _remoteSyncStatusReporter.SetResultAsync(
                RemoteSyncDisplayState.Error,
                totalProcessed,
                bootstrapExceededMessage,
                cancellationToken);
            Log.Warning(bootstrapExceededMessage);
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            var delay = TimeSpan.FromSeconds(4);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    delay = await RunSingleIterationAsync(stoppingToken);

                    await Task.Delay(delay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                Log.Information(LogMessages.RemoteSyncStopped);
            }
            catch (Exception ex)
            {
                await _remoteSyncStatusReporter.SetLoopFailedAsync(ex.Message, CancellationToken.None);
                Log.Error(ex, LogMessages.RemoteSyncLoopFailed);
            }
        }

        internal async Task<TimeSpan> RunSingleIterationAsync(CancellationToken stoppingToken)
        {
            if (!_remoteSyncOptions.IsConfigured)
            {
                await _remoteSyncStatusReporter.SetDisabledAsync(stoppingToken);
                return TimeSpan.FromSeconds(15);
            }

            await _remoteSyncStatusReporter.SetSyncingAsync(stoppingToken);

            var result = await _remoteSyncService.SyncPendingAsync(stoppingToken);
            await _remoteSyncStatusReporter.SetResultAsync(
                ToDisplayState(result.Status),
                result.ProcessedCount,
                result.ErrorMessage,
                stoppingToken);

            return result.Status switch
            {
                RemoteSyncRunStatus.Synced => TimeSpan.FromSeconds(3),
                RemoteSyncRunStatus.NothingToSync => TimeSpan.FromSeconds(12),
                RemoteSyncRunStatus.BackendUnavailable => TimeSpan.FromSeconds(20),
                RemoteSyncRunStatus.Failed => TimeSpan.FromSeconds(30),
                _ => TimeSpan.FromSeconds(15)
            };
        }

        private static RemoteSyncDisplayState ToDisplayState(RemoteSyncRunStatus status)
            => status switch
            {
                RemoteSyncRunStatus.Synced => RemoteSyncDisplayState.Synced,
                RemoteSyncRunStatus.NothingToSync => RemoteSyncDisplayState.Synced,
                RemoteSyncRunStatus.BackendUnavailable => RemoteSyncDisplayState.Offline,
                RemoteSyncRunStatus.Failed => RemoteSyncDisplayState.Error,
                _ => RemoteSyncDisplayState.Disabled
            };
    }
}
