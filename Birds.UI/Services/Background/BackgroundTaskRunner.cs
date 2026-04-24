using Microsoft.Extensions.Logging;

namespace Birds.UI.Services.Background;

public sealed class BackgroundTaskRunner(ILogger<BackgroundTaskRunner> logger) : IBackgroundTaskRunner
{
    private readonly ILogger<BackgroundTaskRunner> _logger = logger;

    public void Run(
        Func<CancellationToken, Task> operation,
        BackgroundTaskOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(options);

        // Intentionally detached: RunCoreAsync observes every terminal state and logs unexpected failures.
        _ = RunCoreAsync(operation, options, cancellationToken);
    }

    private async Task RunCoreAsync(
        Func<CancellationToken, Task> operation,
        BackgroundTaskOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Background task {OperationName} was canceled.", options.OperationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background task {OperationName} failed.", options.OperationName);
            NotifyFailure(options, ex);
        }
    }

    private void NotifyFailure(BackgroundTaskOptions options, Exception exception)
    {
        if (options.OnFailure is null)
            return;

        try
        {
            options.OnFailure(exception);
        }
        catch (Exception notifyException)
        {
            _logger.LogError(
                notifyException,
                "Failure notification for background task {OperationName} failed.",
                options.OperationName);
        }
    }
}
