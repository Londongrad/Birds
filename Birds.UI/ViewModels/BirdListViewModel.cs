using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.UI.Enums;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Views.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

public partial class BirdListViewModel : ObservableObject
{
    private readonly IBirdManager _birdManager;
    private readonly IBirdNameDisplayService _birdNameDisplay;
    private readonly ILocalizationService _localization;

    [ObservableProperty] private IReadOnlyList<FilterOption> filters = Array.Empty<FilterOption>();

    [ObservableProperty] private string? searchText;

    [ObservableProperty] private FilterOption selectedFilter = null!;

    public BirdListViewModel(IBirdManager birdManager,
        ILocalizationService localization,
        IBirdNameDisplayService birdNameDisplay)
    {
        _birdManager = birdManager;
        _localization = localization;
        _birdNameDisplay = birdNameDisplay;

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

        if (Birds is INotifyCollectionChanged birdsChanged)
            birdsChanged.CollectionChanged += OnBirdsCollectionChanged;

        _localization.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<BirdDTO> Birds { get; }
    public static Array BirdNames => Enum.GetValues(typeof(BirdsName));
    public ICollectionView BirdsView { get; }

    public bool IsLoading => _birdManager.Store.LoadState == LoadState.Loading;

    public bool IsFailed => _birdManager.Store.LoadState == LoadState.Failed;

    public int BirdCount => BirdsView.Cast<object>().Count();

    partial void OnSelectedFilterChanged(FilterOption value)
    {
        BirdsView.Refresh();
        OnPropertyChanged(nameof(BirdCount));
    }

    partial void OnSearchTextChanged(string? value)
    {
        BirdsView.Refresh();
        OnPropertyChanged(nameof(BirdCount));
    }

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

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedFilter = SelectedFilter;

        Filters = CreateFilters();
        SelectedFilter =
            Filters.FirstOrDefault(x => x.Filter == selectedFilter.Filter && x.Species == selectedFilter.Species)
            ?? Filters[0];

        BirdsView.Refresh();
        OnPropertyChanged(nameof(BirdCount));
    }

    private IReadOnlyList<FilterOption> CreateFilters()
    {
        var filters = new List<FilterOption>
        {
            new(BirdFilter.All, AppText.Get("BirdList.Filter.All")),
            new(BirdFilter.Alive, AppText.Get("BirdList.Filter.Alive")),
            new(BirdFilter.Dead, AppText.Get("BirdList.Filter.Dead")),
            new(BirdFilter.DepartedButAlive, AppText.Get("BirdList.Filter.Released"))
        };

        filters.AddRange(Enum.GetValues<BirdsName>()
            .Select(species => new FilterOption(BirdFilter.BySpecies, _birdNameDisplay.GetDisplayName(species),
                species)));

        return filters;
    }

    private bool MatchesSearchText(BirdDTO bird)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var text = SearchText.Trim();
        var species = BirdEnumHelper.ParseBirdName(bird.Name);
        var localizedName = species.HasValue ? _birdNameDisplay.GetDisplayName(species.Value) : bird.Name;

        return localizedName.Contains(text, StringComparison.CurrentCultureIgnoreCase)
               || bird.Name?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true
               || _localization.FormatDate(bird.Arrival).Contains(text, StringComparison.CurrentCultureIgnoreCase)
               || (bird.Departure is { } departure && _localization.FormatDate(departure)
                   .Contains(text, StringComparison.CurrentCultureIgnoreCase))
               || bird.Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    private void OnBirdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(BirdCount));
    }
}
