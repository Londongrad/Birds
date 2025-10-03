using Birds.Application.DTOs;
using Birds.Application.Notifications;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Enums;
using Birds.UI.Enums;
using Birds.UI.Services.Navigation;
using Birds.UI.Services.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject,
                                             INotificationHandler<BirdCreatedNotification>,
                                             INotificationHandler<BirdDeletedNotification>,
                                             IAsyncNavigatedTo
    {
        public BirdListViewModel(IMediator mediator, INotificationService notification)
        {
            _mediator = mediator;
            _notification = notification;

            BirdsView = CollectionViewSource.GetDefaultView(Birds);
            BirdsView.Filter = FilterBirds;

            SelectedFilter = Filters.First();
        }

        #region [ Fields ]

        private readonly IMediator _mediator;
        private readonly INotificationService _notification;
        private bool _isLoaded; // Флаг: выполнена ли начальная загрузка (чтобы не грузить повторно)
        private int _isLoading; // Флаг: выполняется ли загрузка в данный момент (0 = нет, 1 = да)

        #endregion [ Fields ]

        #region [ Properties ]

        public ObservableCollection<BirdDTO> Birds { get; } = new();
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));
        public ICollectionView BirdsView { get; }
        public List<FilterOption> Filters { get; } =
        [
            new(BirdFilter.All, "Показать всех"),
            new(BirdFilter.Alive, "Показать только живых"),
            new(BirdFilter.Dead, "Только мертвые"),
            new(BirdFilter.DepartedButAlive, "Отпущенные"),
            new(BirdFilter.Amadin, "Амадин"),
            new(BirdFilter.Sparrow, "Воробей"),
            new(BirdFilter.GreatTit, "Большак"),
            new(BirdFilter.Chickadee, "Гайка"),
            new(BirdFilter.Nuthatch, "Поползень"),
            new(BirdFilter.Goldfinch, "Щегол"),
            new(BirdFilter.Grosbeak, "Дубонос")
        ];

        #endregion [ Properties ]

        #region [ ObservableProperties ]

        [ObservableProperty] private FilterOption selectedFilter;

        #endregion [ ObservableProperties ]

        #region [ Methods ]

        /// <summary>
        /// Загружает всех птиц из БД.
        /// Используется защита от параллельных вызовов (Interlocked).
        /// </summary>
        private async Task LoadAsync()
        {
            // Interlocked предотвращает гонку условий при одновременных вызовах
            if (Interlocked.Exchange(ref _isLoading, 1) == 1) return;
            try
            {
                Birds.Clear();
                var result = await _mediator.Send(new GetAllBirdsQuery());

                foreach (var bird in result)
                    Birds.Add(bird);

                _isLoaded = true;
            }
            finally
            {
                Interlocked.Exchange(ref _isLoading, 0);
            }
        }

        /// <summary>
        /// Вызывает загрузку только один раз при первом переходе на эту ViewModel.
        /// </summary>
        public async Task OnNavigatedToAsync()
        {
            if (!_isLoaded)
            {
                await LoadAsync();
            }
        }

        /// <summary>
        /// Событие изменения фильтра. Обновляет представление. Вызывается из Mvvm.Toolkit
        /// </summary>
        /// <param name="value"></param>
        partial void OnSelectedFilterChanged(FilterOption value)
        {
            BirdsView.Refresh();
        }

        /// <summary>
        /// Сортировка коллекции по фильтру
        /// </summary>
        public bool FilterBirds(object obj)
        {
            if (obj is not BirdDTO bird) return false;

            return SelectedFilter.Filter switch
            {
                BirdFilter.All => true,
                BirdFilter.Alive => bird.IsAlive,
                BirdFilter.Dead => !bird.IsAlive,
                BirdFilter.DepartedButAlive => bird.IsAlive && bird.Departure is not null,
                BirdFilter.Amadin => bird.Name == "Амадин",
                BirdFilter.Sparrow => bird.Name == "Воробей",
                BirdFilter.GreatTit => bird.Name == "Большак",
                BirdFilter.Chickadee => bird.Name == "Гайка",
                BirdFilter.Nuthatch => bird.Name == "Поползень",
                BirdFilter.Goldfinch => bird.Name == "Щегол",
                BirdFilter.Grosbeak => bird.Name == "Дубонос",
                _ => true
            };
        }

        #endregion [ Methods ]

        #region [ Handlers ]

        /// <summary>
        /// Добавление созданной птицы в коллекцию по уведомлению.
        /// На случай вызова не из UI потока, переключаемся на него.
        /// </summary>
        public Task Handle(BirdCreatedNotification notification, CancellationToken cancellationToken)
        {
            var d = System.Windows.Application.Current.Dispatcher;
            if (d.CheckAccess())
                Birds.Add(notification.Bird); // Уже на UI-потоке — добавляем напрямую
            else
                d.BeginInvoke(() => Birds.Add(notification.Bird)); // Переключаемся на UI-поток
            return Task.CompletedTask;
        }

        /// <summary>
        /// Удаление птицы из коллекции по уведомлению.
        /// На случай вызова не из UI потока, переключаемся на него.
        /// </summary>
        public Task Handle(BirdDeletedNotification notification, CancellationToken cancellationToken)
        {
            var d = System.Windows.Application.Current.Dispatcher;
            if (d.CheckAccess())
            {
                var vm = Birds.FirstOrDefault(x => x.Id == notification.BirdId);
                if (vm != null) Birds.Remove(vm);
            }
            else
            {
                d.BeginInvoke(() =>
                {
                    var vm = Birds.FirstOrDefault(x => x.Id == notification.BirdId);
                    if (vm != null) Birds.Remove(vm);
                });
            }
            _notification.ShowSuccess("Bird was successfully removed");

            return Task.CompletedTask;
        }

        #endregion [ Handlers ]
    }
}