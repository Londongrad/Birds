using Birds.Application.Commands.CreateBird;
using Birds.Application.Notifications;
using Birds.UI.Services;
using Birds.UI.ViewModels;
using Birds.UI.Views.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.UI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUI(this IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<BirdListViewModel>();
            services.AddSingleton<AddBirdViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<INotificationHandler<BirdCreatedNotification>>(sp =>
                sp.GetRequiredService<BirdListViewModel>());

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(CreateBirdCommandHandler).Assembly);
            });

            return services;
        }
    }
}
