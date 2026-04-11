using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Extensions;
using Birds.Shared.Localization;
using Birds.UI.Enums;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace Birds.UI.ViewModels
{
    public record StatItem(string Label, int Count);
    public record YearFilterOption(int? Year, string Label);

    public partial class BirdStatisticsViewModel : ObservableObject
    {
        [ObservableProperty] private int totalBirds;
        [ObservableProperty] private int aliveCount;
        [ObservableProperty] private int releasedCount;
        [ObservableProperty] private int killCount;

        [ObservableProperty] private string averageKeeping = "\u2014";
        [ObservableProperty] private string? topMonth;
        [ObservableProperty] private string? topWeek;
        [ObservableProperty] private string? topDay;
        [ObservableProperty] private string? longestBreak;

        [ObservableProperty] private int? selectedYear;
        [ObservableProperty] private IReadOnlyCollection<int> availableYears = Array.Empty<int>();
        [ObservableProperty] private IReadOnlyCollection<YearFilterOption> yearFilters = Array.Empty<YearFilterOption>();
        [ObservableProperty] private YearFilterOption? selectedYearFilter;

        public ObservableCollection<BirdDTO> Birds { get; }
        public ObservableCollection<StatItem> SpeciesStats { get; } = new();
        public ObservableCollection<StatItem> YearStats { get; } = new();
        public ObservableCollection<StatItem> MonthStats { get; } = new();
        public ObservableCollection<StatItem> LongestKeepingStats { get; } = new();

        public bool IsLoading => _birdStore.LoadState == LoadState.Loading;
        public bool HasBirds => TotalBirds > 0;
        public string FilterSummary => SelectedYear is int year
            ? _localization.GetString("Statistics.FilterSummary.Year", year)
            : _localization.GetString("Statistics.FilterSummary.All");

        private readonly IBirdStore _birdStore;
        private readonly ILocalizationService _localization;

        public BirdStatisticsViewModel(IBirdStore birdStore, ILocalizationService localization)
        {
            _birdStore = birdStore;
            _localization = localization;
            Birds = birdStore.Birds;

            Birds.CollectionChanged += OnBirdsChanged;
            Recalculate();

            _birdStore.PropertyChanged += (s, e) =>
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

        private void OnBirdsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Recalculate();
        }

        partial void OnSelectedYearChanged(int? value)
        {
            UpdateSelectedYearFilter();
            OnPropertyChanged(nameof(FilterSummary));
            Recalculate();
        }

        partial void OnSelectedYearFilterChanged(YearFilterOption? value)
        {
            var year = value?.Year;
            if (SelectedYear != year)
                SelectedYear = year;
        }

        partial void OnTotalBirdsChanged(int value) => OnPropertyChanged(nameof(HasBirds));

        [RelayCommand] private void ClearYearFilter() => SelectedYear = null;

        private void Recalculate()
        {
            var filteredBirds = FilterBirdsByYear();
            CalculateCounts(filteredBirds);
            UpdateSpeciesStats(filteredBirds);
            UpdateYearStats();
            UpdateMonthStats(filteredBirds);
            UpdateAvailableYears();
            CalculateAdditionalMetrics(filteredBirds);
            CalculateLongestKeeping(filteredBirds);
        }

        private IList<BirdDTO> FilterBirdsByYear()
        {
            IEnumerable<BirdDTO> q = Birds;

            if (SelectedYear is int y)
                q = q.Where(b => b.Arrival.Year == y);

            return q as IList<BirdDTO> ?? q.ToList();
        }

        private void CalculateCounts(IList<BirdDTO> filteredBirds)
        {
            int total = 0, released = 0, killed = 0;
            int alive = Birds.Count(b => b.IsAlive && b.Departure == null);

            foreach (var b in filteredBirds)
            {
                total++;

                if (b.Departure != null && b.IsAlive)
                    released++;
                else if (!b.IsAlive)
                    killed++;
            }

            TotalBirds = total;
            AliveCount = alive;
            ReleasedCount = released;
            KillCount = killed;
        }

        private void UpdateSpeciesStats(IList<BirdDTO> filteredBirds)
        {
            var byName = filteredBirds
                .GroupBy(b => BirdEnumHelper.ParseBirdName(b.Name)?.ToDisplayName() ?? b.Name);

            SpeciesStats.Clear();
            foreach (var group in byName.OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
                SpeciesStats.Add(new StatItem(group.Key, group.Count()));
        }

        private void UpdateYearStats()
        {
            YearStats.Clear();
            foreach (var g in Birds.GroupBy(b => b.Arrival.Year).OrderByDescending(g => g.Key))
                YearStats.Add(new StatItem(g.Key.ToString(), g.Count()));
        }

        private void UpdateMonthStats(IList<BirdDTO> filteredBirds)
        {
            MonthStats.Clear();
            foreach (var g in filteredBirds
                .GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month))
            {
                var firstOfMonth = new DateOnly(g.Key.Year, g.Key.Month, 1);
                var label = _localization.FormatDate(firstOfMonth, DateDisplayStyle.MonthYearShort);
                MonthStats.Add(new StatItem(label, g.Count()));
            }
        }

        private void UpdateAvailableYears()
        {
            AvailableYears = new SortedSet<int>(Birds.Select(b => b.Arrival.Year));
            RebuildYearFilters();
        }

        private void CalculateAdditionalMetrics(IList<BirdDTO> filteredBirds)
        {
            if (filteredBirds.Count == 0)
            {
                AverageKeeping = TopMonth = TopWeek = TopDay = LongestBreak = "\u2014";
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var averageKeepingDays = filteredBirds
                .Select(b =>
                {
                    var end = b.Departure ?? today;
                    return Math.Max(0, (end.ToDateTime(TimeOnly.MinValue) - b.Arrival.ToDateTime(TimeOnly.MinValue)).TotalDays);
                })
                .DefaultIfEmpty(0)
                .Average();
            AverageKeeping = _localization.GetString(
                "Statistics.AverageKeepingValue",
                (int)Math.Round(averageKeepingDays, MidpointRounding.AwayFromZero));

            var topMonthGroup = filteredBirds
                .GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (topMonthGroup is not null)
            {
                var firstOfMonth = new DateOnly(topMonthGroup.Key.Year, topMonthGroup.Key.Month, 1);
                var label = _localization.FormatDate(firstOfMonth, DateDisplayStyle.MonthYearLong);
                TopMonth = $"{label} \u2013 {AppText.Format("Statistics.CountBirds", topMonthGroup.Count())}";
            }
            else
            {
                TopMonth = "\u2014";
            }

            var topWeekGroup = filteredBirds
                .GroupBy(b =>
                {
                    var dt = b.Arrival.ToDateTime(TimeOnly.MinValue);
                    return new
                    {
                        Year = ISOWeek.GetYear(dt),
                        Week = ISOWeek.GetWeekOfYear(dt)
                    };
                })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (topWeekGroup is not null)
            {
                int year = topWeekGroup.Key.Year;
                int week = topWeekGroup.Key.Week;
                string range = FormatIsoWeekRange(year, week);
                TopWeek = $"{range} \u2013 {AppText.Format("Statistics.CountBirds", topWeekGroup.Count())}";
            }
            else
            {
                TopWeek = "\u2014";
            }

            var topDayGroup = filteredBirds
                .GroupBy(b => b.Arrival)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            TopDay = topDayGroup != null
                ? $"{_localization.FormatDate(topDayGroup.Key, DateDisplayStyle.Long)} \u2013 {AppText.Format("Statistics.CountBirds", topDayGroup.Count())}"
                : "\u2014";

            CalculateLongestBreak(filteredBirds);
        }

        private void CalculateLongestBreak(IList<BirdDTO> filteredBirds)
        {
            var orderedDates = filteredBirds
                .Select(b => b.Arrival)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (orderedDates.Count <= 1)
            {
                LongestBreak = "\u2014";
                return;
            }

            var maxGap = TimeSpan.Zero;
            DateOnly? start = null;
            DateOnly? end = null;

            for (int i = 1; i < orderedDates.Count; i++)
            {
                var gap = orderedDates[i].ToDateTime(TimeOnly.MinValue) -
                          orderedDates[i - 1].ToDateTime(TimeOnly.MinValue);

                if (gap > maxGap)
                {
                    maxGap = gap;
                    start = orderedDates[i - 1];
                    end = orderedDates[i];
                }
            }

            LongestBreak = AppText.Format(
                _localization.CurrentCulture,
                "Statistics.LongestBreakValue",
                maxGap.TotalDays - 1,
                _localization.FormatDate(start),
                _localization.FormatDate(end));
        }

        private void CalculateLongestKeeping(IList<BirdDTO> filteredBirds)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var byName = filteredBirds.GroupBy(b => BirdEnumHelper.ParseBirdName(b.Name)?.ToDisplayName() ?? b.Name);

            LongestKeepingStats.Clear();

            foreach (var group in byName)
            {
                var maxDays = group
                    .Select(b =>
                    {
                        var end = b.Departure ?? today;
                        return (end.ToDateTime(TimeOnly.MinValue) - b.Arrival.ToDateTime(TimeOnly.MinValue)).TotalDays;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                LongestKeepingStats.Add(new StatItem(group.Key, (int)maxDays));
            }

            var ordered = LongestKeepingStats
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label)
                .ToList();

            LongestKeepingStats.Clear();
            foreach (var item in ordered)
                LongestKeepingStats.Add(item);
        }

        private string FormatIsoWeekRange(int isoYear, int isoWeek)
        {
            var start = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
            var end = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Sunday));
            return $"{_localization.FormatDate(start, DateDisplayStyle.Medium)}\u2013{_localization.FormatDate(end, DateDisplayStyle.Medium)}";
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
}
