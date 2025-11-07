using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;

namespace Birds.App;

/// <summary>
/// Centralized Serilog setup: bootstrap logger for very-early startup
/// and full host-integrated configuration with file sinks.
/// </summary>
internal static class SerilogSetup
{
    /// <summary>
    /// Minimal bootstrap logger so early startup messages are not lost
    /// before the Host is built.
    /// </summary>
    public static void InitBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Debug() // visible in VS Output
            .CreateLogger();
    }

    /// <summary>
    /// Full Serilog configuration used by Host. Reads levels/enrichers from appsettings
    /// and appends a file sink with an absolute path based on the environment.
    /// </summary>
    public static void Configure(HostBuilderContext ctx, IServiceProvider services, LoggerConfiguration cfg)
    {
        var env = ctx.HostingEnvironment;

        // Dev: <project>\logs ; Prod: %LOCALAPPDATA%\Birds\logs
        var root = env.IsDevelopment()
            ? env.ContentRootPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birds");

        var logsDir = Path.Combine(root, "logs");
        Directory.CreateDirectory(logsDir);

        cfg.ReadFrom.Configuration(ctx.Configuration)   // levels/overrides/enrichers from appsettings
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .WriteTo.File(
              path: Path.Combine(logsDir, "app-.log"),
              rollingInterval: RollingInterval.Day,
              retainedFileCountLimit: 10,
              rollOnFileSizeLimit: true,
              shared: true,
              outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
          .WriteTo.Debug(); // keep logs in VS Output too (optional)
    }
}
