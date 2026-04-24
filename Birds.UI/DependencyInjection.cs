using Birds.Shared.Sync;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Caching;
using Birds.UI.Services.Dialogs;
using Birds.UI.Services.Dialogs.Interfaces;
using Birds.UI.Services.Export;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Import;
using Birds.UI.Services.Import.Interfaces;
using Birds.UI.Services.Background;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Navigation;
using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Search;
using Birds.UI.Services.Shell;
using Birds.UI.Services.Shell.Interfaces;
using Birds.UI.Services.Statistics;
using Birds.UI.Services.Statistics.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Services.Sync;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using Birds.UI.Threading;
using Birds.UI.Threading.Abstractions;
using Birds.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Birds.UI;

public static class DependencyInjection
{
    public static void AddUI(this IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<BirdListViewModel>();
        services.AddSingleton<AddBirdViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<BirdStatisticsViewModel>();
        services.AddSingleton<AppearanceSettingsViewModel>();
        services.AddSingleton<ImportExportSettingsViewModel>();
        services.AddSingleton<SyncSettingsViewModel>();
        services.AddSingleton<DangerZoneSettingsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Services
        services.AddSingleton<IBackgroundTaskRunner, BackgroundTaskRunner>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IBirdViewModelFactory, BirdViewModelFactory>();
        services.AddSingleton<IBirdViewModelCache>(sp =>
            new BirdViewModelCache(sp.GetRequiredService<IBirdViewModelFactory>()));
        services.AddSingleton<IBirdStore, BirdStore>();
        services.AddSingleton<BirdStoreInitializer>();
        services.AddSingleton<IBirdManager, BirdManager>();
        services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
        services.AddSingleton<INotificationManager, NotificationManager>();
        services.AddSingleton<RemoteSyncStatusStore>();
        services.AddSingleton<IRemoteSyncStatusSource>(sp => sp.GetRequiredService<RemoteSyncStatusStore>());
        services.AddSingleton<IRemoteSyncStatusReporter>(sp => sp.GetRequiredService<RemoteSyncStatusStore>());
        services.AddSingleton<ILocalizationService>(_ => LocalizationService.Instance);
        services.AddSingleton<IBirdNameDisplayService, BirdNameDisplayService>();
        services.AddSingleton<IBirdSearchMatcher, BirdSearchMatcher>();
        services.AddSingleton<IAppPreferencesPathProvider, LocalAppPreferencesPathProvider>();
        services.AddSingleton<IAppPreferencesService, JsonAppPreferencesService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDataFileDialogService, DataFileDialogService>();
        services.AddSingleton<IImportService, JsonImportService>();
        services.AddSingleton<IPathNavigationService, PathNavigationService>();
        services.AddSingleton<IBirdStatisticsCalculator, BirdStatisticsCalculator>();

        // Export Services
        services.AddSingleton<IExportService, JsonExportService>();
        services.AddSingleton<IAutoExportCoordinator, AutoExportCoordinator>();
    }
}
