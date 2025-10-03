using Birds.Application.Notifications;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Navigation;
using Birds.UI.Services.Notification;
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
            services.AddTransient<IBirdViewModelFactory, BirdViewModelFactory>();

            services.AddSingleton<INotificationHandler<BirdCreatedNotification>>(sp =>
                sp.GetRequiredService<BirdListViewModel>());

            services.AddSingleton<INotificationHandler<BirdDeletedNotification>>(sp =>
                sp.GetRequiredService<BirdListViewModel>());

            services.AddSingleton<INotificationHandler<NavigatedEvent>>(sp =>
                (NotificationService)sp.GetRequiredService<INotificationService>());

            return services;
        }
    }
}