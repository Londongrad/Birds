using Birds.App.Services;
using Birds.Application;
using Birds.Infrastructure;
using Birds.Infrastructure.Configuration;
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
        /// Thrown if the configured database provider, seeding mode, or connection string is invalid.
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

                    // Register the Application layer (CQRS, Mediator, validators, etc.)
                    services.AddApplication();

                    var provider = ResolveDatabaseProvider(context.Configuration);
                    var connectionString = ResolveConnectionString(context.Configuration, provider);
                    var seedingOptions = ResolveSeedingOptions(context.Configuration);

                    // Register the Infrastructure layer (EF Core, DbContext, repositories, etc.)
                    services.AddInfrastructure(provider, connectionString, seedingOptions);

                    // Register the UI layer (ViewModels, stores, navigation, etc.)
                    services.AddUI();
                })
                .Build();
        }

        private static DatabaseProvider ResolveDatabaseProvider(IConfiguration configuration)
        {
            var configuredProvider = configuration["Database:Provider"];
            if (string.IsNullOrWhiteSpace(configuredProvider))
                return DatabaseProvider.Sqlite;

            if (Enum.TryParse<DatabaseProvider>(configuredProvider, ignoreCase: true, out var provider))
                return provider;

            throw new ConfigurationErrorsException(ErrorMessages.InvalidDatabaseProvider(configuredProvider));
        }

        private static string ResolveConnectionString(IConfiguration configuration, DatabaseProvider provider)
        {
            var configuredConnectionName = configuration["Database:ConnectionStringName"];
            string[] candidateNames = string.IsNullOrWhiteSpace(configuredConnectionName)
                ? provider switch
                {
                    DatabaseProvider.Sqlite => ["Sqlite", "DefaultConnection"],
                    DatabaseProvider.Postgres => ["Postgres", "DefaultConnection"],
                    _ => ["DefaultConnection"]
                }
                : [configuredConnectionName];

            foreach (var name in candidateNames)
            {
                var rawConnection = configuration.GetConnectionString(name);
                if (!string.IsNullOrWhiteSpace(rawConnection))
                    return ReplaceEnvPlaceholders(rawConnection);
            }

            throw new ConfigurationErrorsException(ErrorMessages.ConnectionStringNotFoundFor(candidateNames));
        }

        private static DatabaseSeedingOptions ResolveSeedingOptions(IConfiguration configuration)
        {
            var configuredMode = configuration["Seeding:Mode"];
            var mode = string.IsNullOrWhiteSpace(configuredMode)
                ? DatabaseSeedingMode.None
                : Enum.TryParse<DatabaseSeedingMode>(configuredMode, ignoreCase: true, out var parsedMode)
                    ? parsedMode
                    : throw new ConfigurationErrorsException(ErrorMessages.InvalidDatabaseSeedingMode(configuredMode));

            var recordCount = Math.Max(0, configuration.GetValue<int?>("Seeding:RecordCount") ?? 20_000);
            var batchSize = Math.Max(1, configuration.GetValue<int?>("Seeding:BatchSize") ?? 500);
            var randomSeed = Math.Max(0, configuration.GetValue<int?>("Seeding:RandomSeed") ?? 42);

            return new DatabaseSeedingOptions(mode, recordCount, batchSize, randomSeed);
        }
    }
}
