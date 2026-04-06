using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Birds.Infrastructure.Services
{
    public sealed class DatabaseInitializerHostedService(
        IDbContextFactory<BirdDbContext> contextFactory,
        DatabaseRuntimeOptions options,
        ILogger<DatabaseInitializerHostedService> logger) : IHostedService
    {
        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;
        private readonly DatabaseRuntimeOptions _options = options;
        private readonly ILogger<DatabaseInitializerHostedService> _logger = logger;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            switch (_options.Provider)
            {
                case DatabaseProvider.Sqlite:
                    await context.Database.EnsureCreatedAsync(cancellationToken);
                    _logger.LogInformation("SQLite database is ready at {ConnectionString}.", _options.ConnectionString);
                    break;

                case DatabaseProvider.Postgres:
                    if (await context.Database.CanConnectAsync(cancellationToken))
                    {
                        await context.Database.MigrateAsync(cancellationToken);
                        _logger.LogInformation("PostgreSQL database is reachable and migrations are applied.");
                    }
                    break;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
