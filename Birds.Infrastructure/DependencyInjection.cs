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
            services.AddDbContextFactory<BirdDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddSingleton<IBirdRepository, BirdRepository>();
            services.AddSingleton<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}