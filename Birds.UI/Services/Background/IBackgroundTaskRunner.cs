namespace Birds.UI.Services.Background;

public interface IBackgroundTaskRunner
{
    void Run(
        Func<CancellationToken, Task> operation,
        BackgroundTaskOptions options,
        CancellationToken cancellationToken = default);
}
