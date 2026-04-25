using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Birds.Infrastructure.Configuration;
using Birds.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Birds.App.Services;

[method: ActivatorUtilitiesConstructor]
internal sealed class DiagnosticsService(
    ILogger<DiagnosticsService> logger,
    DatabaseRuntimeOptions databaseOptions,
    IRemoteSyncRuntimeOptionsProvider remoteSyncOptionsProvider) : IDiagnosticsService
{
    public DiagnosticsService(
        ILogger<DiagnosticsService> logger,
        DatabaseRuntimeOptions databaseOptions,
        RemoteSyncRuntimeOptions remoteSyncOptions)
        : this(
            logger,
            databaseOptions,
            new StaticRemoteSyncRuntimeOptionsProvider(remoteSyncOptions))
    {
    }

    public string LogDirectory
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SerilogSetup.CurrentLogsDirectory))
                return SerilogSetup.CurrentLogsDirectory;

            return AppLogPathResolver.ResolveLogsDirectory();
        }
    }

    public string CurrentLogFilePath => Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");

    public StartupDiagnosticSnapshot CaptureStartupDiagnostics()
    {
        var assembly = typeof(App).Assembly.GetName();
        var offset = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now);

        return new StartupDiagnosticSnapshot(
            assembly.Version?.ToString() ?? "unknown",
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            CultureInfo.CurrentCulture.Name,
            CultureInfo.CurrentUICulture.Name,
            TimeZoneInfo.Local.Id,
            FormatUtcOffset(offset),
            databaseOptions.Provider.ToString(),
            ResolveSafeDatabasePath(),
            remoteSyncOptionsProvider.Current.IsEnabled,
            remoteSyncOptionsProvider.Current.IsConfigured,
            remoteSyncOptionsProvider.Current.HasConfigurationError,
            remoteSyncOptionsProvider.Current.HasConfigurationError
                ? remoteSyncOptionsProvider.Current.ConfigurationErrorMessage
                : null,
            LogDirectory,
            CurrentLogFilePath);
    }

    public void LogStartupDiagnostics()
    {
        var snapshot = CaptureStartupDiagnostics();

        logger.LogInformation(
            LogMessages.StartupDiagnostics,
            snapshot.AppVersion,
            snapshot.OsVersion,
            snapshot.RuntimeVersion,
            snapshot.ProcessArchitecture,
            snapshot.Culture,
            snapshot.UiCulture,
            snapshot.TimeZone,
            snapshot.UtcOffset,
            snapshot.DatabaseProvider,
            snapshot.DatabasePath,
            snapshot.RemoteSyncEnabled,
            snapshot.RemoteSyncConfigured,
            snapshot.RemoteSyncHasConfigurationError,
            snapshot.RemoteSyncConfigurationError,
            snapshot.LogDirectory,
            snapshot.CurrentLogFilePath);
    }

    public bool OpenLogDirectory()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = LogDirectory,
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to open diagnostics log directory {LogDirectory}.", LogDirectory);
            return false;
        }
    }

    private string ResolveSafeDatabasePath()
    {
        if (databaseOptions.Provider == DatabaseProvider.Sqlite)
            return DiagnosticRedactor.TryGetSqliteDataSource(databaseOptions.ConnectionString)
                   ?? DiagnosticRedactor.RedactConnectionString(databaseOptions.ConnectionString);

        return DiagnosticRedactor.RedactConnectionString(databaseOptions.ConnectionString);
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return $"{sign}{offset.Duration():hh\\:mm}";
    }
}
