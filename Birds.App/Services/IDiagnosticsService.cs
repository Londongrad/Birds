namespace Birds.App.Services;

internal interface IDiagnosticsService
{
    string LogDirectory { get; }

    string CurrentLogFilePath { get; }

    StartupDiagnosticSnapshot CaptureStartupDiagnostics();

    void LogStartupDiagnostics();

    bool OpenLogDirectory();
}

internal sealed record StartupDiagnosticSnapshot(
    string AppVersion,
    string OsVersion,
    string RuntimeVersion,
    string ProcessArchitecture,
    string Culture,
    string UiCulture,
    string TimeZone,
    string UtcOffset,
    string DatabaseProvider,
    string DatabasePath,
    bool RemoteSyncEnabled,
    bool RemoteSyncConfigured,
    bool RemoteSyncHasConfigurationError,
    string? RemoteSyncConfigurationError,
    string LogDirectory,
    string CurrentLogFilePath);
