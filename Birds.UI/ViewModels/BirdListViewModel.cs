using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.UI.Enums;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Views.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Birds.UI.ViewModels
{
    /// <summary>
    /// ViewModel responsible for displaying and filtering the bird collection in the UI.
    ///
    /// <para>
    /// This ViewModel does not perform any data loading or modification itself.
    /// Instead, it uses the shared <see cref="IBirdManager"/> and its <see cref="IBirdStore"/>
    /// to access the observable bird collection for presentation.
    /// </para>
    ///
    /// <para>
    /// It provides sorting, filtering, and search capabilities for the existing collection,
    /// while all CRUD operations and reloading logic are handled in <see cref="IBirdManager"/>.
    /// </para>
    /// </summary>
    public partial class BirdListViewModel : ObservableObject
    {
        public BirdListViewModel(IBirdManager birdManager)
        {
            _birdManager = birdManager;

            // Get the shared bird collection from the store
            Birds = birdManager.Store.Birds;

            // Collection view for UI binding with sorting and filtering
            BirdsView = new ListCollectionView(Birds)
            {
                CustomSort = new BirdComparer(),
                Filter = FilterBirds,
            };
            SelectedFilter = Filters.First();

            birdManager.Store.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(birdManager.Store.LoadState))
                    OnPropertyChanged(nameof(IsLoading));
            };
        }

        #region [ Fields ]

        private readonly IBirdManager _birdManager;

        #endregion [ Fields ]

        #region [ Properties ]

        public ObservableCollection<BirdDTO> Birds { get; }
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));
        public ICollectionView BirdsView { get; }

        /// <summary>Data loading indicator from BirdStore.</summary>
        public bool IsLoading => _birdManager.Store.LoadState == LoadState.Loading;
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
    }
}