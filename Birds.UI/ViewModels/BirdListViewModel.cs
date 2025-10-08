using Birds.Application.DTOs;
using Birds.Application.Notifications;
using Birds.Domain.Enums;
using Birds.UI.Enums;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject,
                                             INotificationHandler<BirdCreatedNotification>,
                                             INotificationHandler<BirdDeletedNotification>
    {
        public BirdListViewModel(
            IMediator mediator,
            INotificationService notification,
            IBirdStore birdStore)
        {
            _mediator = mediator;
            _notification = notification;

            // Получаем ссылку на коллекцию птиц из BirdStore
            Birds = birdStore.Birds;

            // ICollectionView с фильтром для UI
            BirdsView = CollectionViewSource.GetDefaultView(Birds);
            SelectedFilter = Filters.First();
            BirdsView.Filter = FilterBirds;

        }

        #region [ Fields ]

        private readonly IMediator _mediator;
        private readonly INotificationService _notification;

        #endregion [ Fields ]

        #region [ Properties ]

        public ObservableCollection<BirdDTO> Birds { get; }
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
            if (SelectedFilter is null) return true;

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
        public async Task Handle(BirdCreatedNotification notification, CancellationToken cancellationToken)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(() =>
            {
                if (notification.Bird != null) Birds.Add(notification.Bird);
            });
        }

        /// <summary>
        /// Удаление птицы из коллекции по уведомлению.
        /// На случай вызова не из UI потока, переключаемся на него.
        /// </summary>
        public async Task Handle(BirdDeletedNotification notification, CancellationToken cancellationToken)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(() =>
            {
                var vm = Birds.FirstOrDefault(x => x.Id == notification.BirdId);
                if (vm != null) Birds.Remove(vm);
            });

            _notification.ShowSuccess("Bird was successfully removed");
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