using Birds.Application.Interfaces;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Repositories;
using Birds.Infrastructure.Seeding;
using Birds.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services,
        string localStoreConnectionString,
        DatabaseSeedingOptions seedingOptions,
        RemoteSyncRuntimeOptions remoteSyncOptions)
    {
        var normalizedConnectionString = NormalizeSqliteConnectionString(localStoreConnectionString);

        services.AddSingleton(new DatabaseRuntimeOptions(DatabaseProvider.Sqlite, normalizedConnectionString));
        services.AddSingleton(seedingOptions);
        services.AddSingleton(remoteSyncOptions);

        // Register a factory so each repository call can create its own short-lived DbContext.
        services.AddDbContextFactory<BirdDbContext>(options =>
            options.UseSqlite(normalizedConnectionString));

        services.AddSingleton<BirdSeeder>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializerService>();
        services.AddSingleton<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        services.AddSingleton<ILocalStoreStateService, LocalStoreStateService>();
        services.AddSingleton<IBirdRepository, BirdRepository>();

        if (remoteSyncOptions.IsConfigured)
        {
            services.AddDbContextFactory<RemoteBirdDbContext>(options =>
                options.UseNpgsql(remoteSyncOptions.ConnectionString!, n => n.EnableRetryOnFailure(0)));
            services.AddSingleton<IRemoteSyncService, RemoteSyncService>();
        }
        else
        {
            services.AddSingleton<IRemoteSyncService, DisabledRemoteSyncService>();
        }
    }

    private static string NormalizeSqliteConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
            return builder.ToString();

        if (!Path.IsPathRooted(builder.DataSource))
            builder.DataSource = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, builder.DataSource));

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        return builder.ToString();
    }
}