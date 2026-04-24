using Birds.App.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Birds.Tests.App.Services;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public void Handle_Should_Log_Exception_And_Return_Safe_User_Message()
    {
        var logger = new TestLogger<GlobalExceptionHandler>();
        var diagnostics = new TestDiagnosticsService(Path.Combine("C:", "Users", "Test", "AppData", "Local", "Birds", "Logs"));
        var sut = new GlobalExceptionHandler(logger, diagnostics);
        var exception = new InvalidOperationException("database password raw stack detail");

        var result = sut.Handle(exception, "UI Dispatcher", GlobalExceptionSeverity.Fatal);

        result.ShouldShutdown.Should().BeTrue();
        result.UserMessage.Should().Contain(diagnostics.LogDirectory);
        result.UserMessage.Should().NotContain("database password raw stack detail");
        result.UserMessage.Should().NotContain("InvalidOperationException");
        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error
            && entry.Exception == exception
            && entry.Message.Contains("UI Dispatcher", StringComparison.Ordinal));
    }

    [Fact]
    public void Handle_Should_Not_Request_Shutdown_For_Recoverable_Exception()
    {
        var sut = new GlobalExceptionHandler(
            new TestLogger<GlobalExceptionHandler>(),
            new TestDiagnosticsService(Path.Combine("C:", "Logs")));

        var result = sut.Handle(
            new InvalidOperationException("background failure"),
            "Unobserved Task",
            GlobalExceptionSeverity.Recoverable);

        result.ShouldShutdown.Should().BeFalse();
    }

    private sealed class TestDiagnosticsService(string logDirectory) : IDiagnosticsService
    {
        public string LogDirectory { get; } = logDirectory;

        public string CurrentLogFilePath => Path.Combine(LogDirectory, "app-test.log");

        public StartupDiagnosticSnapshot CaptureStartupDiagnostics()
        {
            return new StartupDiagnosticSnapshot(
                "test",
                "test",
                "test",
                "test",
                "en-US",
                "en-US",
                "test",
                "+00:00",
                "Sqlite",
                "birds.db",
                false,
                false,
                false,
                null,
                LogDirectory,
                CurrentLogFilePath);
        }

        public void LogStartupDiagnostics()
        {
        }

        public bool OpenLogDirectory()
        {
            return true;
        }
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
