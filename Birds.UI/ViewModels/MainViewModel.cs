using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Birds.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;
        private readonly INotificationManager _notificationManager;

        public MainViewModel(INavigationService navigation,
                             INotificationManager notificationManager,
                             BirdListViewModel birdsVM,
                             AddBirdViewModel addBirdVM,
                             BirdStatisticsViewModel birdStatistics)
        {
            _navigation = navigation;
            _notificationManager = notificationManager;

            _navigation.AddCreator<BirdListViewModel>(() => birdsVM);
            _navigation.AddCreator<AddBirdViewModel>(() => addBirdVM);
            _navigation.AddCreator<BirdStatisticsViewModel>(() => birdStatistics);

            if (_navigation is INotifyPropertyChanged navigationNotify)
                navigationNotify.PropertyChanged += OnNavigationPropertyChanged;

            _navigation.NavigateTo(addBirdVM);
            UpdateHeader(addBirdVM.GetType());
        }

        public INavigationService Navigation => _navigation;

        public ReadOnlyObservableCollection<NotificationToast> Notifications => _notificationManager.ActiveNotifications;

        [ObservableProperty]
        private string headerTitle = "Добавление птицы";

        [ObservableProperty]
        private string headerSubtitle = "Новая запись с базовой валидацией и чистой формой ввода.";

        [ObservableProperty]
        private bool showContentHeader;

        [RelayCommand]
        private void DismissNotification(NotificationToast? notification)
        {
            if (notification is null)
                return;

            _notificationManager.DismissNotification(notification);
        }

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(INavigationService.Current))
                return;

            UpdateHeader(_navigation.Current?.GetType());
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
                default:
                    ShowContentHeader = true;
                    HeaderTitle = "Birds";
                    HeaderSubtitle = "Рабочее пространство приложения.";
                    break;
            }
        }
    }
}
