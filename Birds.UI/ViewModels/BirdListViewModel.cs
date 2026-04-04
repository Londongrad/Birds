using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.UI.Enums;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Views.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private readonly IBirdManager _birdManager;

        public BirdListViewModel(IBirdManager birdManager)
        {
            _birdManager = birdManager;

            Birds = birdManager.Store.Birds;
            Filters = CreateFilters();

            BirdsView = new ListCollectionView(Birds)
            {
                CustomSort = new BirdComparer(),
                Filter = FilterBirds
            };

            SelectedFilter = Filters[0];

            birdManager.Store.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(birdManager.Store.LoadState))
                {
                    OnPropertyChanged(nameof(IsLoading));
                    OnPropertyChanged(nameof(IsFailed));
                    ReloadBirdsCommand.NotifyCanExecuteChanged();
                }
            };
        }

        public ObservableCollection<BirdDTO> Birds { get; }
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));
        public ICollectionView BirdsView { get; }

        /// <summary>Data loading indicator from BirdStore.</summary>
        public bool IsLoading => _birdManager.Store.LoadState == LoadState.Loading;

        /// <summary>True when loading failed and a retry could be useful.</summary>
        public bool IsFailed => _birdManager.Store.LoadState == LoadState.Failed;

        public IReadOnlyList<FilterOption> Filters { get; }

        [ObservableProperty] private FilterOption selectedFilter = null!;
        [ObservableProperty] private string? searchText;

        partial void OnSelectedFilterChanged(FilterOption value)
        {
            BirdsView.Refresh();
        }

        partial void OnSearchTextChanged(string? value)
        {
            BirdsView.Refresh();
        }

        /// <summary>
        /// Filters the bird collection according to the selected filter and search text.
        /// </summary>
        /// <param name="obj">The object to be checked against the current filter.</param>
        /// <returns><see langword="true"/> if the bird matches the filter and search criteria; otherwise, <see langword="false"/>.</returns>
        public bool FilterBirds(object obj)
        {
            if (obj is not BirdDTO bird)
                return false;

            if (!MatchesSearchText(bird))
                return false;

            if (SelectedFilter is null)
                return true;

            return SelectedFilter.Filter switch
            {
                BirdFilter.All => true,
                BirdFilter.Alive => bird.IsAlive && bird.Departure is null,
                BirdFilter.Dead => !bird.IsAlive,
                BirdFilter.DepartedButAlive => bird.IsAlive && bird.Departure is not null,
                BirdFilter.BySpecies => BirdEnumHelper.ParseBirdName(bird.Name) == SelectedFilter.Species,
                _ => true
            };
        }

        [RelayCommand]
        private async Task ReloadBirdsAsync()
        {
            await _birdManager.ReloadAsync(CancellationToken.None);
        }

        private static IReadOnlyList<FilterOption> CreateFilters()
        {
            var filters = new List<FilterOption>
            {
                new(BirdFilter.All, "Показать всех"),
                new(BirdFilter.Alive, "Показать только живых"),
                new(BirdFilter.Dead, "Только мертвые"),
                new(BirdFilter.DepartedButAlive, "Отпущенные")
            };

            filters.AddRange(Enum.GetValues<BirdsName>().Select(FilterOption.SpeciesFilter));

            return filters;
        }

        private bool MatchesSearchText(BirdDTO bird)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var text = SearchText.Trim();

            return (bird.Name?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true)
                || (bird.Arrival.ToString().Contains(text, StringComparison.CurrentCultureIgnoreCase) == true)
                || (bird.Departure?.ToString().Contains(text, StringComparison.CurrentCultureIgnoreCase) == true)
                || (bird.Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true);
        }
    }
}
