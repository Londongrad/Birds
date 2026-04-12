using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Birds.Application.DTOs;
using Birds.Shared.Localization;
using Birds.UI.Enums;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Statistics;
using Birds.UI.Services.Statistics.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

public record StatItem(string Label, int Count);
public record StatBarItem(string Label, int Count, double Ratio);
public record YearFilterOption(int? Year, string Label);

public partial class BirdStatisticsViewModel : ObservableObject
{
    private readonly IBirdStore _birdStore;
    private readonly IBirdStatisticsCalculator _calculator;
    private readonly ILocalizationService _localization;

    [ObservableProperty] private int aliveCount;
    [ObservableProperty] private int arrivalsLast30Days;
    [ObservableProperty] private IReadOnlyCollection<int> availableYears = Array.Empty<int>();
    [ObservableProperty] private string averageKeeping = "\u2014";
    [ObservableProperty] private int departuresLast30Days;
    [ObservableProperty] private int killCount;
    [ObservableProperty] private string? longestBreak;
    [ObservableProperty] private string longestActiveKeeping = "\u2014";
    [ObservableProperty] private string medianKeeping = "\u2014";
    [ObservableProperty] private int peakConcurrentCount;
    [ObservableProperty] private int releasedCount;
    [ObservableProperty] private int? selectedYear;
    [ObservableProperty] private YearFilterOption? selectedYearFilter;
    [ObservableProperty] private string? topDay;
    [ObservableProperty] private string? topMonth;
    [ObservableProperty] private string? topWeek;
    [ObservableProperty] private int totalBirds;
    [ObservableProperty] private IReadOnlyCollection<YearFilterOption> yearFilters = Array.Empty<YearFilterOption>();

    public BirdStatisticsViewModel(IBirdStore birdStore, ILocalizationService localization)
        : this(birdStore, localization, new BirdStatisticsCalculator(localization))
    {
    }

    public BirdStatisticsViewModel(
        IBirdStore birdStore,
        ILocalizationService localization,
        IBirdStatisticsCalculator calculator)
    {
        _birdStore = birdStore;
        _localization = localization;
        _calculator = calculator;
        Birds = birdStore.Birds;

        Birds.CollectionChanged += OnBirdsChanged;
        Recalculate();

        _birdStore.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_birdStore.LoadState))
                OnPropertyChanged(nameof(IsLoading));
        };

        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FilterSummary));
            Recalculate();
        };
    }

    public ObservableCollection<BirdDTO> Birds { get; }
    public ObservableCollection<StatItem> SpeciesStats { get; } = new();
    public ObservableCollection<StatItem> YearStats { get; } = new();
    public ObservableCollection<StatItem> MonthStats { get; } = new();
    public ObservableCollection<StatItem> LongestKeepingStats { get; } = new();
    public ObservableCollection<StatBarItem> TopSpeciesStats { get; } = new();
    public ObservableCollection<StatBarItem> YearDistributionStats { get; } = new();
    public ObservableCollection<StatBarItem> MonthOfYearStats { get; } = new();
    public ObservableCollection<StatBarItem> DepartureMonthOfYearStats { get; } = new();

    public bool IsLoading => _birdStore.LoadState == LoadState.Loading;
    public bool HasBirds => TotalBirds > 0;

    public string FilterSummary => SelectedYear is int year
        ? _localization.GetString("Statistics.FilterSummary.Year", year)
        : _localization.GetString("Statistics.FilterSummary.All");

    private void OnBirdsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Recalculate();
    }

    partial void OnSelectedYearChanged(int? value)
    {
        OnPropertyChanged(nameof(FilterSummary));
        Recalculate();
    }

    partial void OnSelectedYearFilterChanged(YearFilterOption? value)
    {
        var year = value?.Year;
        if (SelectedYear != year)
            SelectedYear = year;
    }

    partial void OnTotalBirdsChanged(int value)
    {
        OnPropertyChanged(nameof(HasBirds));
    }

    [RelayCommand]
    private void ClearYearFilter()
    {
        SelectedYear = null;
    }

    private void Recalculate()
    {
        var snapshot = _calculator.Calculate(Birds, SelectedYear);

        TotalBirds = snapshot.TotalBirds;
        AliveCount = snapshot.AliveCount;
        ReleasedCount = snapshot.ReleasedCount;
        KillCount = snapshot.KillCount;
        ArrivalsLast30Days = snapshot.ArrivalsLast30Days;
        DeparturesLast30Days = snapshot.DeparturesLast30Days;
        PeakConcurrentCount = snapshot.PeakConcurrentCount;
        AverageKeeping = snapshot.AverageKeeping;
        MedianKeeping = snapshot.MedianKeeping;
        LongestActiveKeeping = snapshot.LongestActiveKeeping;
        TopMonth = snapshot.TopMonth;
        TopWeek = snapshot.TopWeek;
        TopDay = snapshot.TopDay;
        LongestBreak = snapshot.LongestBreak;
        AvailableYears = snapshot.AvailableYears;

        ReplaceItems(SpeciesStats, snapshot.SpeciesStats);
        ReplaceItems(YearStats, snapshot.YearStats);
        ReplaceItems(MonthStats, snapshot.MonthStats);
        ReplaceItems(LongestKeepingStats, snapshot.LongestKeepingStats);
        ReplaceItems(TopSpeciesStats, snapshot.TopSpeciesStats);
        ReplaceItems(YearDistributionStats, snapshot.YearDistributionStats);
        ReplaceItems(MonthOfYearStats, snapshot.MonthOfYearStats);
        ReplaceItems(DepartureMonthOfYearStats, snapshot.DepartureMonthOfYearStats);

        RebuildYearFilters();
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();

        foreach (var item in items)
            target.Add(item);
    }

    private void RebuildYearFilters()
    {
        YearFilters =
        [
            new YearFilterOption(null, _localization.GetString("Statistics.AllYears")),
            .. AvailableYears
                .OrderByDescending(x => x)
                .Select(x => new YearFilterOption(x, x.ToString(CultureInfo.InvariantCulture)))
        ];

        UpdateSelectedYearFilter();
    }

    private void UpdateSelectedYearFilter()
    {
        SelectedYearFilter = YearFilters.FirstOrDefault(x => x.Year == SelectedYear)
                             ?? YearFilters.FirstOrDefault(x => x.Year is null);
    }
}
