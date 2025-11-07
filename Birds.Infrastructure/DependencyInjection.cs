using Birds.Application.Interfaces;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<BirdDbContext>(options =>
                options.UseNpgsql(connectionString, n => n.EnableRetryOnFailure(0)), ServiceLifetime.Singleton);

            services.AddSingleton<IBirdRepository, BirdRepository>();

            return services;
        }
    }
}
