using Birds.Application.Interfaces;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Repositories;
using Birds.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services,
                                             DatabaseProvider provider,
                                             string connectionString)
        {
            var normalizedConnectionString = provider switch
            {
                DatabaseProvider.Sqlite => NormalizeSqliteConnectionString(connectionString),
                _ => connectionString
            };

            services.AddSingleton(new DatabaseRuntimeOptions(provider, normalizedConnectionString));

            // Register a factory so each repository call can create its own short-lived DbContext.
            services.AddDbContextFactory<BirdDbContext>(options =>
            {
                switch (provider)
                {
                    case DatabaseProvider.Sqlite:
                        options.UseSqlite(normalizedConnectionString);
                        break;

                    case DatabaseProvider.Postgres:
                        options.UseNpgsql(normalizedConnectionString, n => n.EnableRetryOnFailure(0));
                        break;
                }
            });

            services.AddHostedService<DatabaseInitializerHostedService>();
            services.AddSingleton<IBirdRepository, BirdRepository>();
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
}
