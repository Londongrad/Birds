using Birds.Application.DTOs;
using Birds.Application.Notifications;
using Birds.Domain.Enums;
using Birds.UI.Enums;
using Birds.UI.Extensions;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Views.Helpers;
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
                                             INotificationHandler<BirdUpdatedNotification>
    {
        public BirdListViewModel(
            INotificationService notification,
            IBirdStore birdStore)
        {
            _notification = notification;
            _birdStore = birdStore;

            // Получаем ссылку на коллекцию птиц из BirdStore
            Birds = birdStore.Birds;

            // Представление для UI
            BirdsView = new ListCollectionView(Birds)
            {
                CustomSort = new BirdComparer(),
                Filter = FilterBirds,
            };
            SelectedFilter = Filters.First();

            _birdStore.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_birdStore.IsLoading))
                    OnPropertyChanged(nameof(IsLoading));
            };
        }

        #region [ Fields ]

        private readonly INotificationService _notification;
        private readonly IBirdStore _birdStore;

        #endregion [ Fields ]

        #region [ Properties ]

        public ObservableCollection<BirdDTO> Birds { get; }
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));
        public ICollectionView BirdsView { get; }

        /// <summary>Data loading indicator from BirdStore.</summary>
        public bool IsLoading => _birdStore.IsLoading;
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
        [ObservableProperty] private string? searchText;

        #endregion [ ObservableProperties ]

        #region [ Events ]

        /// <summary>
        /// Triggered when the filter option changes. Updates the view.
        /// Automatically invoked by the Mvvm.Toolkit source generator.
        /// </summary>
        /// <param name="value">The newly selected filter option.</param>
        partial void OnSelectedFilterChanged(FilterOption value)
        {
            BirdsView.Refresh();
        }

        /// <summary>
        /// Triggered when the search text changes. Updates the view.
        /// Automatically invoked by the Mvvm.Toolkit source generator.
        /// </summary>
        /// <param name="value">The new search text value.</param>
        partial void OnSearchTextChanged(string? value)
        {
            BirdsView.Refresh();
        }

        #endregion [ Events ]

        #region [ Methods ]

        /// <summary>
        /// Filters the bird collection according to the selected filter and search text.
        /// </summary>
        /// <param name="obj">The object to be checked against the current filter.</param>
        /// <returns><see langword="true"/> if the bird matches the filter and search criteria; otherwise, <see langword="false"/>.</returns>
        public bool FilterBirds(object obj)
        {
            if (obj is not BirdDTO bird)
                return false;

            // Check search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var text = SearchText.Trim();

                bool matchesSearch =
                    (bird.Name?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true)
                    || (bird.Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true);

                if (!matchesSearch)
                    return false;
            }

            // If no filter is selected, show all birds
            if (SelectedFilter is null)
                return true;

            // Apply the selected filter
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

        #region [ Notification Handlers ]

        /// <summary>
        /// Adds the newly created bird to the collection when a notification is received.
        /// If invoked from a non-UI thread, switches to the UI thread.
        /// </summary>
        public async Task Handle(BirdCreatedNotification notification, CancellationToken cancellationToken)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(() =>
            {
                if (notification.Bird != null)
                    Birds.Add(notification.Bird);
            });
        }

        /// <summary>
        /// Removes a bird from the collection when a notification is received.
        /// If invoked from a non-UI thread, switches to the UI thread.
        /// </summary>
        public async Task Handle(BirdDeletedNotification notification, CancellationToken cancellationToken)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(() =>
            {
                var vm = Birds.FirstOrDefault(x => x.Id == notification.BirdId);
                if (vm != null)
                    Birds.Remove(vm);
            });

            _notification.ShowSuccess("Bird was successfully removed");
        }

        /// <summary>
        /// Updates an existing bird in the collection when a notification is received.
        /// If invoked from a non-UI thread, switches to the UI thread.
        /// </summary>
        public async Task Handle(BirdUpdatedNotification notification, CancellationToken cancellationToken)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(() =>
            {
                Birds.ReplaceOrAdd(b => b.Id == notification.BirdDTO.Id, notification.BirdDTO);
            });

            _notification.ShowSuccess("Bird was successfully updated");
        }

        #endregion [ Notification Handlers ]
    }
}