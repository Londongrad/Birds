using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.UI.Enums;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Caching;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Views.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

public partial class BirdListViewModel : ObservableObject, IDisposable
{
    private readonly IBirdManager _birdManager;
    private readonly IBirdNameDisplayService _birdNameDisplay;
    private readonly IBirdViewModelCache _birdViewModelCache;
    private readonly ILocalizationService _localization;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _disposed;
    private CancellationTokenSource? _reloadCancellation;

    [ObservableProperty] private IReadOnlyList<FilterOption> filters = Array.Empty<FilterOption>();

    [ObservableProperty] private string? searchText;

    [ObservableProperty] private FilterOption selectedFilter = null!;

    public BirdListViewModel(IBirdManager birdManager,
        ILocalizationService localization,
        IBirdNameDisplayService birdNameDisplay,
        IBirdViewModelCache birdViewModelCache)
    {
        _birdManager = birdManager;
        _localization = localization;
        _birdNameDisplay = birdNameDisplay;
        _birdViewModelCache = birdViewModelCache;

        Birds = birdManager.Store.Birds;
        Filters = CreateFilters();

        BirdsView = new ListCollectionView(Birds)
        {
            CustomSort = new BirdComparer(),
            Filter = FilterBirds
        };

        SelectedFilter = Filters[0];

        birdManager.Store.PropertyChanged += OnStorePropertyChanged;

        if (Birds is INotifyCollectionChanged birdsChanged)
            birdsChanged.CollectionChanged += OnBirdsCollectionChanged;

        _localization.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<BirdDTO> Birds { get; }
    public static Array BirdNames => Enum.GetValues(typeof(BirdSpecies));
    public ICollectionView BirdsView { get; }
    public IBirdViewModelCache BirdViewModelCache => _birdViewModelCache;

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
            BirdFilter.BySpecies => bird.ResolveSpecies() == SelectedFilter.Species,
            _ => true
        };
    }

    [RelayCommand]
    private async Task ReloadBirdsAsync(CancellationToken cancellationToken)
    {
        var operationCancellation = CreateReloadCancellation(cancellationToken);

        try
        {
            await _birdManager.ReloadAsync(operationCancellation.Token);
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // A newer reload superseded this one or the view model is being disposed.
        }
        finally
        {
            ClearReloadCancellation(operationCancellation);
        }
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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _reloadCancellation?.Cancel();
        _birdManager.Store.PropertyChanged -= OnStorePropertyChanged;

        if (Birds is INotifyCollectionChanged birdsChanged)
            birdsChanged.CollectionChanged -= OnBirdsCollectionChanged;

        _localization.LanguageChanged -= OnLanguageChanged;
        _birdViewModelCache.Dispose();
        _lifetimeCancellation.Dispose();
    }

    private CancellationTokenSource CreateReloadCancellation(CancellationToken cancellationToken)
    {
        var previous = _reloadCancellation;
        var current = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        _reloadCancellation = current;

        previous?.Cancel();
        previous?.Dispose();

        return current;
    }

    private void ClearReloadCancellation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_reloadCancellation, operationCancellation))
            _reloadCancellation = null;

        operationCancellation.Dispose();
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

        filters.AddRange(Enum.GetValues<BirdSpecies>()
            .Select(species => new FilterOption(BirdFilter.BySpecies, _birdNameDisplay.GetDisplayName(species),
                species)));

        return filters;
    }

    private bool MatchesSearchText(BirdDTO bird)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var text = SearchText.Trim();
        var species = bird.ResolveSpecies();
        var localizedName = species.HasValue ? _birdNameDisplay.GetDisplayName(species.Value) : bird.Name;

        return localizedName.Contains(text, StringComparison.CurrentCultureIgnoreCase)
               || bird.Name?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true
               || _localization.FormatDate(bird.Arrival).Contains(text, StringComparison.CurrentCultureIgnoreCase)
               || (bird.Departure is { } departure && _localization.FormatDate(departure)
                   .Contains(text, StringComparison.CurrentCultureIgnoreCase))
               || bird.Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    private void OnStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(_birdManager.Store.LoadState))
            return;

        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsFailed));
        ReloadBirdsCommand.NotifyCanExecuteChanged();
    }

    private void OnBirdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
                RemoveCachedBirds(e.OldItems);
                break;
            case NotifyCollectionChangedAction.Replace:
                RemoveReplacedCachedBirds(e.OldItems, e.NewItems);
                RefreshCachedBirds(e.NewItems);
                break;
            case NotifyCollectionChangedAction.Reset:
                _birdViewModelCache.Clear();
                break;
        }

        OnPropertyChanged(nameof(BirdCount));
    }

    private void RemoveReplacedCachedBirds(System.Collections.IList? oldItems, System.Collections.IList? newItems)
    {
        if (oldItems is null)
            return;

        var newIds = newItems?.OfType<BirdDTO>().Select(bird => bird.Id).ToHashSet() ?? [];

        foreach (var oldBird in oldItems.OfType<BirdDTO>())
            if (!newIds.Contains(oldBird.Id))
                _birdViewModelCache.Remove(oldBird.Id);
    }

    private void RemoveCachedBirds(System.Collections.IList? birds)
    {
        if (birds is null)
            return;

        foreach (var bird in birds.OfType<BirdDTO>())
            _birdViewModelCache.Remove(bird.Id);
    }

    private void RefreshCachedBirds(System.Collections.IList? birds)
    {
        if (birds is null)
            return;

        foreach (var bird in birds.OfType<BirdDTO>())
            _birdViewModelCache.Refresh(bird);
    }
}
