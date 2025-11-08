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
            // Register DbContext with Singleton lifetime and disable retry on failure
            services.AddDbContext<BirdDbContext>(options =>
                options.UseNpgsql(connectionString, n => n.EnableRetryOnFailure(0)), ServiceLifetime.Singleton);

            services.AddSingleton<IBirdRepository, BirdRepository>();
        }
    }
}
