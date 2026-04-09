using Birds.Shared.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Birds.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;
        private readonly INotificationManager _notificationManager;
        private readonly IAppPreferencesService _appPreferences;
        private readonly IThemeService _themeService;
        private readonly ILocalizationService _localization;
        private Type? _currentViewModelType;

        public MainViewModel(INavigationService navigation,
                             INotificationManager notificationManager,
                             IAppPreferencesService appPreferences,
                             IThemeService themeService,
                             ILocalizationService localization,
                             BirdListViewModel birdsVM,
                             AddBirdViewModel addBirdVM,
                             BirdStatisticsViewModel birdStatistics,
                             SettingsViewModel settingsViewModel)
        {
            _navigation = navigation;
            _notificationManager = notificationManager;
            _appPreferences = appPreferences;
            _themeService = themeService;
            _localization = localization;

            _navigation.AddCreator<BirdListViewModel>(() => birdsVM);
            _navigation.AddCreator<AddBirdViewModel>(() => addBirdVM);
            _navigation.AddCreator<BirdStatisticsViewModel>(() => birdStatistics);
            _navigation.AddCreator<SettingsViewModel>(() => settingsViewModel);

            if (_navigation is INotifyPropertyChanged navigationNotify)
                navigationNotify.PropertyChanged += OnNavigationPropertyChanged;

            if (_notificationManager is INotifyPropertyChanged notificationNotify)
                notificationNotify.PropertyChanged += OnNotificationManagerPropertyChanged;

            if (_appPreferences is INotifyPropertyChanged preferencesNotify)
                preferencesNotify.PropertyChanged += OnPreferencesPropertyChanged;

            if (_notificationManager.ActiveNotifications is INotifyCollectionChanged notificationsChanged)
                notificationsChanged.CollectionChanged += OnNotificationsCollectionChanged;

            _localization.LanguageChanged += OnLanguageChanged;

            _localization.ApplyLanguage(_appPreferences.SelectedLanguage);
            _themeService.ApplyTheme(_appPreferences.SelectedTheme);

            _navigation.NavigateTo(addBirdVM);
            _currentViewModelType = addBirdVM.GetType();
            UpdateHeader(_currentViewModelType);
        }

        public INavigationService Navigation => _navigation;

        public ReadOnlyObservableCollection<NotificationToast> Notifications => _notificationManager.ActiveNotifications;

        public int UnreadNotificationCount => _notificationManager.UnreadCount;

        public bool HasNotifications => _notificationManager.HasNotifications;

        public bool HasUnreadNotifications => UnreadNotificationCount > 0;

        public bool ShouldShowNotificationBadge => _appPreferences.ShowNotificationBadge && HasUnreadNotifications;

        public bool HasRecentOperationStatus => _notificationManager.HasRecentOperationStatus;

        public bool IsRecentOperationSuccess => _notificationManager.RecentOperationStatusType == NotificationType.Success;

        public bool IsRecentOperationError => _notificationManager.RecentOperationStatusType == NotificationType.Error;

        public string RecentOperationStatusToolTip =>
            IsRecentOperationError
                ? AppText.Get("Notification.QuickStatus.Error")
                : AppText.Get("Notification.QuickStatus.Success");

        [ObservableProperty]
        private string headerTitle = AppText.Get("Main.Header.AddBird.Title");

        [ObservableProperty]
        private string headerSubtitle = AppText.Get("Main.Header.AddBird.Subtitle");

        [ObservableProperty]
        private bool showContentHeader;

        [ObservableProperty]
        private bool isNotificationCenterOpen;

        [RelayCommand]
        private void ToggleNotificationCenter()
        {
            IsNotificationCenterOpen = !IsNotificationCenterOpen;

            if (IsNotificationCenterOpen)
                _notificationManager.MarkAllAsRead();
        }

        [RelayCommand]
        private void DismissNotification(NotificationToast? notification)
        {
            if (notification is null)
                return;

            _notificationManager.DismissNotification(notification);
        }

        [RelayCommand]
        private void ClearNotifications()
        {
            _notificationManager.ClearNotifications();
        }

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(INavigationService.Current))
                return;

            _currentViewModelType = _navigation.Current?.GetType();
            UpdateHeader(_currentViewModelType);
        }

        private void OnNotificationManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(INotificationManager.UnreadCount)
                or nameof(INotificationManager.HasNotifications)
                or nameof(INotificationManager.HasRecentOperationStatus)
                or nameof(INotificationManager.RecentOperationStatusType))
            {
                OnPropertyChanged(nameof(UnreadNotificationCount));
                OnPropertyChanged(nameof(HasUnreadNotifications));
                OnPropertyChanged(nameof(HasNotifications));
                OnPropertyChanged(nameof(ShouldShowNotificationBadge));
                OnPropertyChanged(nameof(HasRecentOperationStatus));
                OnPropertyChanged(nameof(IsRecentOperationSuccess));
                OnPropertyChanged(nameof(IsRecentOperationError));
                OnPropertyChanged(nameof(RecentOperationStatusToolTip));
            }
        }

        private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAppPreferencesService.ShowNotificationBadge))
                OnPropertyChanged(nameof(ShouldShowNotificationBadge));
        }

        private void OnNotificationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsNotificationCenterOpen)
                _notificationManager.MarkAllAsRead();

            OnPropertyChanged(nameof(Notifications));
            OnPropertyChanged(nameof(HasNotifications));
            OnPropertyChanged(nameof(ShouldShowNotificationBadge));
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateHeader(_currentViewModelType);
            OnPropertyChanged(nameof(RecentOperationStatusToolTip));
        }

        private void UpdateHeader(Type? currentViewModelType)
        {
            switch (currentViewModelType?.Name)
            {
                case nameof(AddBirdViewModel):
                    ShowContentHeader = false;
                    HeaderTitle = AppText.Get("Main.Header.AddBird.Title");
                    HeaderSubtitle = AppText.Get("Main.Header.AddBird.Subtitle");
                    break;
                case nameof(BirdListViewModel):
                    ShowContentHeader = false;
                    HeaderTitle = AppText.Get("Main.Header.Archive.Title");
                    HeaderSubtitle = AppText.Get("Main.Header.Archive.Subtitle");
                    break;
                case nameof(BirdStatisticsViewModel):
                    ShowContentHeader = true;
                    HeaderTitle = AppText.Get("Main.Header.Statistics.Title");
                    HeaderSubtitle = AppText.Get("Main.Header.Statistics.Subtitle");
                    break;
                case nameof(SettingsViewModel):
                    ShowContentHeader = false;
                    HeaderTitle = AppText.Get("Main.Header.Settings.Title");
                    HeaderSubtitle = AppText.Get("Main.Header.Settings.Subtitle");
                    break;
                default:
                    ShowContentHeader = true;
                    HeaderTitle = AppText.Get("Main.Header.Default.Title");
                    HeaderSubtitle = AppText.Get("Main.Header.Default.Subtitle");
                    break;
            }
        }
    }
}
