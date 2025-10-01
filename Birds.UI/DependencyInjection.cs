using Birds.Application.Notifications;
using Birds.UI.Services;
using Birds.UI.Services.Interfaces;
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
            services.AddSingleton<INotificationService, NotificationService>();

            services.AddSingleton<INotificationHandler<BirdCreatedNotification>>(sp =>
                sp.GetRequiredService<BirdListViewModel>());

            services.AddSingleton<INotificationHandler<BirdDeletedNotification>>(sp =>
                sp.GetRequiredService<BirdListViewModel>());

            return services;
        }
    }
}