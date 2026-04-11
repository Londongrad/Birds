using System.IO;
using Birds.App.Services;
using Birds.Shared.Constants;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Birds.App;

/// <summary>
///     Centralized Serilog setup: bootstrap logger for very-early startup
///     and full host-integrated configuration with file sinks.
/// </summary>
internal static class SerilogSetup
{
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
            .WriteTo.File(
                Path.Combine(logsDir, "bootstrap-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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
            .WriteTo.File(
                Path.Combine(logsDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(logsDir, "error-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                rollOnFileSizeLimit: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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