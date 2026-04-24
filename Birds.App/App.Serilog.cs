using System.IO;
using Birds.App.Services;
using Birds.Shared.Constants;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Birds.App;

/// <summary>
///     Centralized Serilog setup: bootstrap logger for very-early startup
///     and full host-integrated configuration with file sinks.
/// </summary>
internal static class SerilogSetup
{
    private const long LogFileSizeLimitBytes = 10 * 1024 * 1024;

    private const string OutputTemplate =
        "[{UtcTimestamp:yyyy-MM-dd HH:mm:ss.fff zzz} UTC | {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} LOCAL {Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static string CurrentLogsDirectory { get; private set; } = string.Empty;

    /// <summary>
    ///     Minimal bootstrap logger so early startup messages are not lost
    ///     before the Host is built.
    /// </summary>
    public static void InitBootstrapLogger()
    {
        var logsDir = ResolveAndEnsureLogsDirectory(AppContext.BaseDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.With(new UtcTimestampEnricher())
            .WriteTo.File(
                Path.Combine(logsDir, "bootstrap-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5,
                fileSizeLimitBytes: LogFileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: OutputTemplate)
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information(LogMessages.LogsDirectoryResolved, logsDir);
    }

    /// <summary>
    ///     Full Serilog configuration used by Host. Reads levels/enrichers from appsettings
    ///     and appends file sinks with a stable absolute path.
    /// </summary>
    public static void Configure(HostBuilderContext ctx, IServiceProvider services, LoggerConfiguration cfg)
    {
        var logsDir = ResolveAndEnsureLogsDirectory(ctx.HostingEnvironment.ContentRootPath);

        cfg.ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.With(new UtcTimestampEnricher())
            .WriteTo.File(
                Path.Combine(logsDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                fileSizeLimitBytes: LogFileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: OutputTemplate)
            .WriteTo.File(
                Path.Combine(logsDir, "error-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: LogFileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: OutputTemplate)
            .WriteTo.Debug();
    }

    private static string ResolveAndEnsureLogsDirectory(string? startPath)
    {
        var logsDir = AppLogPathResolver.ResolveLogsDirectory(startPath);
        Directory.CreateDirectory(logsDir);
        CurrentLogsDirectory = logsDir;
        return logsDir;
    }
}

internal sealed class UtcTimestampEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UtcTimestamp", DateTimeOffset.UtcNow));
    }
}
