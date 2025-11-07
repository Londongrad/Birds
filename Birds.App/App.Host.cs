using Birds.App.Services;
using Birds.Application;
using Birds.Infrastructure;
using Birds.Shared.Constants;
using Birds.UI;
using Birds.UI.Services.Export.Interfaces;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Configuration;

namespace Birds.App
{
    public partial class App
    {
        /// <summary>
        /// Creates and configures the .NET Generic Host for the application.
        /// Configures Serilog from configuration, loads the .env file and environment
        /// variables, resolves the database connection string with ${VAR} placeholders,
        /// and registers Application/Infrastructure/UI services.
        /// </summary>
        /// <returns>A fully built <see cref="IHost"/>.</returns>
        /// <exception cref="ConfigurationErrorsException">
        /// Thrown if the "DefaultConnection" connection string is missing.
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

                    // Register the Application layer (CQRS, Mediator, validators, etc.)
                    services.AddApplication();

                    // Get the raw connection string (may contain ${VAR} placeholders)
                    var rawConnection = context.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new ConfigurationErrorsException(ErrorMessages.ConnectionStringNotFound);

                    // Replace ${VAR} placeholders with environment variable values
                    var connectionString = ReplaceEnvPlaceholders(rawConnection);

                    // Register the Infrastructure layer (EF Core, DbContext, repositories, etc.)
                    services.AddInfrastructure(connectionString);

                    // Register the UI layer (ViewModels, stores, navigation, etc.)
                    services.AddUI();
                })
                .Build();
        }
    }
}
