using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Navigation;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.UI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUI(this IServiceCollection services)
        {
            // ViewModels
            services.AddSingleton<BirdListViewModel>();
            services.AddSingleton<AddBirdViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<BirdStatisticsViewModel>();

            // Services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IBirdViewModelFactory, BirdViewModelFactory>();
            services.AddSingleton<IBirdStore, BirdStore>();
            services.AddSingleton<BirdStoreInitializer>();
            services.AddSingleton<IBirdManager, BirdManager>();

            // MediatR notification Handlers
            services.AddSingleton<INotificationHandler<NavigatedEvent>>(sp =>
                (NotificationService)sp.GetRequiredService<INotificationService>());

            return services;
        }
    }
}