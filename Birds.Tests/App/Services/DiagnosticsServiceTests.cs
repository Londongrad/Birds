using Birds.App.Services;
using Birds.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Birds.Tests.App.Services;

public sealed class DiagnosticsServiceTests
{
    [Fact]
    public void CaptureStartupDiagnostics_Should_Resolve_Development_Log_Path_Under_Repository()
    {
        var previousLogDirectory = Environment.GetEnvironmentVariable("BIRDS_LOG_DIR");

        try
        {
            Environment.SetEnvironmentVariable("BIRDS_LOG_DIR", null);
            var databasePath = Path.Combine(Path.GetTempPath(), "birds.db");
            var sut = CreateService($"Data Source={databasePath};Cache=Shared");

            var snapshot = sut.CaptureStartupDiagnostics();

            snapshot.LogDirectory.Should().Be(Path.Combine(FindRepositoryRoot(), "logs"));
            snapshot.CurrentLogFilePath.Should().StartWith(snapshot.LogDirectory);
            snapshot.DatabaseProvider.Should().Be(DatabaseProvider.Sqlite.ToString());
            snapshot.DatabasePath.Should().Be(databasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BIRDS_LOG_DIR", previousLogDirectory);
        }
    }

    [Fact]
    public void LogStartupDiagnostics_Should_Log_Safe_Metadata_Without_Remote_Secrets()
    {
        var logger = new TestLogger<DiagnosticsService>();
        var remoteOptions = new RemoteSyncRuntimeOptions(
            true,
            "Host=db;Database=birds;Username=user;Password=remote-secret");
        var sut = new DiagnosticsService(
            logger,
            new DatabaseRuntimeOptions(DatabaseProvider.Sqlite, "Data Source=birds.db"),
            remoteOptions);

        sut.LogStartupDiagnostics();

        logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Information);
        var message = logger.Entries.Single().Message;
        message.Should().Contain("Startup diagnostics");
        message.Should().Contain("RemoteSyncEnabled=True");
        message.Should().Contain("RemoteSyncConfigured=True");
        message.Should().NotContain("remote-secret");
        message.Should().NotContain("Password=");
    }

    private static DiagnosticsService CreateService(string localConnectionString)
    {
        return new DiagnosticsService(
            new TestLogger<DiagnosticsService>(),
            new DatabaseRuntimeOptions(DatabaseProvider.Sqlite, localConnectionString),
            RemoteSyncRuntimeOptions.Disabled);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Birds.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
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
