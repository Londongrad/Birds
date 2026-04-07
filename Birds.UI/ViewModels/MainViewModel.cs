using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences.Interfaces;
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

        public MainViewModel(INavigationService navigation,
                             INotificationManager notificationManager,
                             IAppPreferencesService appPreferences,
                             BirdListViewModel birdsVM,
                             AddBirdViewModel addBirdVM,
                             BirdStatisticsViewModel birdStatistics,
                             SettingsViewModel settingsViewModel)
        {
            _navigation = navigation;
            _notificationManager = notificationManager;
            _appPreferences = appPreferences;

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

            _navigation.NavigateTo(addBirdVM);
            UpdateHeader(addBirdVM.GetType());
        }

        public INavigationService Navigation => _navigation;

        public ReadOnlyObservableCollection<NotificationToast> Notifications => _notificationManager.ActiveNotifications;

        public int UnreadNotificationCount => _notificationManager.UnreadCount;

        public bool HasNotifications => _notificationManager.HasNotifications;

        public bool HasUnreadNotifications => UnreadNotificationCount > 0;

        public bool ShouldShowNotificationBadge => _appPreferences.ShowNotificationBadge && HasUnreadNotifications;

        [ObservableProperty]
        private string headerTitle = "Добавление птицы";

        [ObservableProperty]
        private string headerSubtitle = "Новая запись с базовой валидацией и чистой формой ввода.";

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

            UpdateHeader(_navigation.Current?.GetType());
        }

        private void OnNotificationManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(INotificationManager.UnreadCount) or nameof(INotificationManager.HasNotifications))
            {
                OnPropertyChanged(nameof(UnreadNotificationCount));
                OnPropertyChanged(nameof(HasUnreadNotifications));
                OnPropertyChanged(nameof(HasNotifications));
                OnPropertyChanged(nameof(ShouldShowNotificationBadge));
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

        private void UpdateHeader(Type? currentViewModelType)
        {
            switch (currentViewModelType?.Name)
            {
                case nameof(AddBirdViewModel):
                    ShowContentHeader = false;
                    HeaderTitle = "Добавление птицы";
                    HeaderSubtitle = "Новая запись с базовой валидацией и чистой формой ввода.";
                    break;
                case nameof(BirdListViewModel):
                    ShowContentHeader = false;
                    HeaderTitle = "Архив птиц";
                    HeaderSubtitle = "Поиск, фильтрация и редактирование существующих записей.";
                    break;
                case nameof(BirdStatisticsViewModel):
                    ShowContentHeader = true;
                    HeaderTitle = "Статистика";
                    HeaderSubtitle = "Сводка по видам, датам и ключевым метрикам.";
                    break;
                case nameof(SettingsViewModel):
                    ShowContentHeader = false;
                    HeaderTitle = "Настройки";
                    HeaderSubtitle = "Пользовательские предпочтения интерфейса и поведения приложения.";
                    break;
                default:
                    ShowContentHeader = true;
                    HeaderTitle = "Birds";
                    HeaderSubtitle = "Рабочее пространство приложения.";
                    break;
            }
        }
    }
}
