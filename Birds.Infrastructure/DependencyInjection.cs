using Birds.Application.Interfaces;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            // Register a factory so each repository call can create its own short-lived DbContext.
            services.AddDbContextFactory<BirdDbContext>(options =>
                options.UseNpgsql(connectionString, n => n.EnableRetryOnFailure(0)));

            services.AddSingleton<IBirdRepository, BirdRepository>();
        }
    }
}
