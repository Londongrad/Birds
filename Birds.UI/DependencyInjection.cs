using Birds.UI.Services.Export;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Dialogs;
using Birds.UI.Services.Dialogs.Interfaces;
using Birds.UI.Services.Import;
using Birds.UI.Services.Import.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Navigation;
using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Sync;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using Birds.UI.Threading;
using Birds.UI.Threading.Abstractions;
using Birds.UI.ViewModels;
using Birds.Shared.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.UI
{
    public static class DependencyInjection
    {
        public static void AddUI(this IServiceCollection services)
        {
            // ViewModels
            services.AddSingleton<BirdListViewModel>();
            services.AddSingleton<AddBirdViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<BirdStatisticsViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // Services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IBirdViewModelFactory, BirdViewModelFactory>();
            services.AddSingleton<IBirdStore, BirdStore>();
            services.AddSingleton<BirdStoreInitializer>();
            services.AddSingleton<IBirdManager, BirdManager>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<INotificationManager, NotificationManager>();
            services.AddSingleton<RemoteSyncStatusStore>();
            services.AddSingleton<IRemoteSyncStatusSource>(sp => sp.GetRequiredService<RemoteSyncStatusStore>());
            services.AddSingleton<IRemoteSyncStatusReporter>(sp => sp.GetRequiredService<RemoteSyncStatusStore>());
            services.AddSingleton<ILocalizationService>(_ => LocalizationService.Instance);
            services.AddSingleton<IAppPreferencesPathProvider, LocalAppPreferencesPathProvider>();
            services.AddSingleton<IAppPreferencesService, JsonAppPreferencesService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDataFileDialogService, DataFileDialogService>();
            services.AddSingleton<IImportService, JsonImportService>();

            // Export Services
            services.AddSingleton<IExportService, JsonExportService>();
            services.AddSingleton<IAutoExportCoordinator, AutoExportCoordinator>();
        }
    }
}
