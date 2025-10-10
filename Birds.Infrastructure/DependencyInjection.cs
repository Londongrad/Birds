using Birds.Application.Interfaces;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Repositories;
using Birds.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Получение строки подключения 
            var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            var dbPath = Path.Combine(projectRoot, "birds.db");

            services.AddDbContext<BirdDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            services.AddScoped<IBirdRepository, BirdRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddHostedService<BirdSeeder>();

            return services;
        }
    }
}