using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Services;
using Birds.Shared.Constants;
using Serilog;

namespace Birds.App.Services
{
    internal sealed class RemoteSyncCoordinator(
        IRemoteSyncService remoteSyncService,
        RemoteSyncRuntimeOptions remoteSyncOptions) : IRemoteSyncCoordinator
    {
        private readonly IRemoteSyncService _remoteSyncService = remoteSyncService;
        private readonly RemoteSyncRuntimeOptions _remoteSyncOptions = remoteSyncOptions;
        private int _started;

        public void Start(CancellationToken stoppingToken)
        {
            if (!_remoteSyncOptions.IsConfigured)
                return;

            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            _ = Task.Run(() => RunAsync(stoppingToken), CancellationToken.None);
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            var delay = TimeSpan.FromSeconds(4);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = await _remoteSyncService.SyncPendingAsync(stoppingToken);

                    delay = result.Status switch
                    {
                        RemoteSyncRunStatus.Synced => TimeSpan.FromSeconds(3),
                        RemoteSyncRunStatus.NothingToSync => TimeSpan.FromSeconds(12),
                        RemoteSyncRunStatus.BackendUnavailable => TimeSpan.FromSeconds(20),
                        RemoteSyncRunStatus.Failed => TimeSpan.FromSeconds(30),
                        _ => TimeSpan.FromSeconds(15)
                    };

                    await Task.Delay(delay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                Log.Information(LogMessages.RemoteSyncStopped);
            }
            catch (Exception ex)
            {
                Log.Error(ex, LogMessages.RemoteSyncLoopFailed);
            }
        }
    }
}
