using System.Configuration;
using Birds.App.Services;
using Birds.Application;
using Birds.Infrastructure;
using Birds.Shared.Sync;
using Birds.UI;
using Birds.UI.Services.Export.Interfaces;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Birds.App;

public partial class App
{
    /// <summary>
    ///     Creates and configures the .NET Generic Host for the application.
    ///     Configures Serilog from configuration, loads the .env file and environment
    ///     variables, resolves the database connection string with ${VAR} placeholders,
    ///     and registers Application/Infrastructure/UI services.
    /// </summary>
    /// <returns>A fully built <see cref="IHost" />.</returns>
    /// <exception cref="ConfigurationErrorsException">
    ///     Thrown if the configured database provider, seeding mode, or connection string is invalid.
    /// </exception>
    internal IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .UseSerilog(SerilogSetup.Configure)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Load .env from the solution root
                Env.TraversePath().Load();
                // Add environment variables so ${...} placeholders can be resolved
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register export path provider (implementation lives in App, interface in UI)
                services.AddSingleton<IExportPathProvider, AppExportPathProvider>();
                services.AddSingleton<StartupDataCoordinator>();
                services.AddSingleton<RemoteSyncCoordinator>();
                services.AddSingleton<IRemoteSyncCoordinator>(sp => sp.GetRequiredService<RemoteSyncCoordinator>());
                services.AddSingleton<IRemoteSyncController>(sp => sp.GetRequiredService<RemoteSyncCoordinator>());

                // Register the Application layer (CQRS, Mediator, validators, etc.)
                services.AddApplication();

                var databaseConfiguration = DatabaseConfigurationResolver.Resolve(context.Configuration);

                // Register the Infrastructure layer (EF Core, DbContext, repositories, etc.)
                services.AddInfrastructure(databaseConfiguration.LocalStoreConnectionString,
                    databaseConfiguration.SeedingOptions,
                    databaseConfiguration.RemoteSync);

                // Register the UI layer (ViewModels, stores, navigation, etc.)
                services.AddUI();
            })
            .Build();
    }
}