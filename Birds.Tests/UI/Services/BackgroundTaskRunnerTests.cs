using Birds.UI.Services.Background;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Birds.Tests.UI.Services;

public sealed class BackgroundTaskRunnerTests
{
    [Fact]
    public async Task Run_Should_Log_Unexpected_Exception_And_Invoke_Failure_Callback()
    {
        var logger = new TestLogger<BackgroundTaskRunner>();
        var sut = new BackgroundTaskRunner(logger);
        var failureObserved = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var exception = new InvalidOperationException("background failed");

        sut.Run(
            _ => throw exception,
            new BackgroundTaskOptions("Failing task", failureObserved.SetResult));

        var observedException = await failureObserved.Task.WaitAsync(TimeSpan.FromSeconds(3));

        observedException.Should().BeSameAs(exception);
        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error
            && entry.Exception == exception
            && entry.Message.Contains("Failing task", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Run_Should_Treat_OperationCanceledException_As_Cancellation()
    {
        var logger = new TestLogger<BackgroundTaskRunner>();
        var sut = new BackgroundTaskRunner(logger);
        var failureCalled = false;

        sut.Run(
            _ => throw new OperationCanceledException(),
            new BackgroundTaskOptions("Canceled task", _ => failureCalled = true));

        await WaitUntilAsync(() => logger.Entries.Any(entry => entry.Level == LogLevel.Debug));

        failureCalled.Should().BeFalse();
        logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Run_Should_Pass_CancellationToken_To_Operation()
    {
        var logger = new TestLogger<BackgroundTaskRunner>();
        var sut = new BackgroundTaskRunner(logger);
        using var cancellation = new CancellationTokenSource();
        var capturedToken = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        sut.Run(
            token =>
            {
                capturedToken.SetResult(token);
                return Task.CompletedTask;
            },
            new BackgroundTaskOptions("Token task"),
            cancellation.Token);

        var token = await capturedToken.Task.WaitAsync(TimeSpan.FromSeconds(3));

        token.Should().Be(cancellation.Token);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(20);
        }

        condition().Should().BeTrue("the expected background task condition should eventually become true");
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
