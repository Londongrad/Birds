using Birds.Application.DTOs;
using Birds.UI.Enums;
using Birds.UI.Services.Stores.BirdStore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace Birds.UI.ViewModels
{
    /// <summary>UI-friendly statistic item.</summary>
    public record StatItem(string Label, int Count);

    public partial class BirdStatisticsViewModel : ObservableObject
    {
        #region [ Observable Properties ]

        // Top metric cards (recomputed with filter applied)
        [ObservableProperty] private int totalBirds;
        [ObservableProperty] private int releasedCount;
        [ObservableProperty] private int killCount;

        [ObservableProperty] private string? topMonth;
        [ObservableProperty] private string? topWeek;
        [ObservableProperty] private string? topDay;
        [ObservableProperty] private string? longestBreak;

        // Filter state
        [ObservableProperty] private int? selectedYear;
        [ObservableProperty] private IReadOnlyCollection<int> availableYears = Array.Empty<int>();

        #endregion [ Observable Properties ]

        #region [ Properties ]

        // Shared UI collection
        public ObservableCollection<BirdDTO> Birds { get; }

        // UI lists
        public ObservableCollection<StatItem> SpeciesStats { get; } = new();
        public ObservableCollection<StatItem> YearStats { get; } = new();
        public ObservableCollection<StatItem> MonthStats { get; } = new();
        public ObservableCollection<StatItem> LongestKeepingStats { get; } = new();

        /// </summary>
        /// <summary>Data loading indicator from BirdStore.</summary>
        public bool IsLoading => _birdStore.LoadState == LoadState.Loading;

        #endregion [ Properties ]

        #region [ Fields ]

        private readonly IBirdStore _birdStore;
        private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        #endregion [ Fields ]

        public BirdStatisticsViewModel(IBirdStore birdStore)
        {
            _birdStore = birdStore;
            Birds = birdStore.Birds;

            // React on collection changes
            Birds.CollectionChanged += OnBirdsChanged;

            // Initial fill
            Recalculate();

            // Subscribe to store property changes to update the UI when IsLoading changes.
            _birdStore.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_birdStore.LoadState))
                    OnPropertyChanged(nameof(IsLoading));
            };
        }

        #region [ Events ]

        private void OnBirdsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Recalculate();
        }

        partial void OnSelectedYearChanged(int? value) => Recalculate();

        #endregion [ Events ]

        #region [ Commands ]

        [RelayCommand] private void ClearYearFilter() => SelectedYear = null;

        #endregion [ Commands ]

        #region [ Methods ]

        private void Recalculate()
        {
            // 1) Data filtering
            var filteredBirds = FilterBirdsByYear();

            // 2) Calculation of main counts
            CalculateCounts(filteredBirds);

            // 3) Grouping by species
            UpdateSpeciesStats(filteredBirds);

            // 4) Grouping by years
            UpdateYearStats();

            // 5) Grouping by months
            UpdateMonthStats(filteredBirds);

            // 6) List of available years
            UpdateAvailableYears();

            // 7) Additional metrics
            CalculateAdditionalMetrics(filteredBirds);

            // 8) The most prolonged keeping by species
            CalculateLongestKeeping(filteredBirds);
        }

        /// <summary> Data filtering </summary>
        private IList<BirdDTO> FilterBirdsByYear()
        {
            IEnumerable<BirdDTO> q = Birds;

            if (SelectedYear is int y)
                q = q.Where(b => b.Arrival.Year == y);

            return q as IList<BirdDTO> ?? q.ToList();
        }

        /// <summary> Calculation of main counts </summary>
        private void CalculateCounts(IList<BirdDTO> filteredBirds)
        {
            int total = 0, released = 0, killed = 0;

            foreach (var b in filteredBirds)
            {
                total++;

                if (b.Departure != null && b.IsAlive is true)
                    released++;
                else if (b.IsAlive is false)
                    killed++;
            }

            TotalBirds = total;
            ReleasedCount = released;
            KillCount = killed;
        }

        /// <summary> Grouping by species </summary>
        private void UpdateSpeciesStats(IList<BirdDTO> filteredBirds)
        {
            var byName = filteredBirds.ToLookup(b => b.Name);

            SpeciesStats.Clear();
            foreach (var group in byName.OrderByDescending(g => g.Count()))
                SpeciesStats.Add(new StatItem(group.Key, group.Count()));
        }

        /// <summary> Grouping by years </summary>
        private void UpdateYearStats()
        {
            YearStats.Clear();
            foreach (var g in Birds.GroupBy(b => b.Arrival.Year).OrderBy(g => g.Key))
                YearStats.Add(new StatItem(g.Key.ToString(), g.Count()));
        }

        /// <summary> Grouping by months </summary>
        private void UpdateMonthStats(IList<BirdDTO> filteredBirds)
        {
            MonthStats.Clear();
            foreach (var g in filteredBirds
                .GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month))
            {
                var firstOfMonth = new DateOnly(g.Key.Year, g.Key.Month, 1);
                var label = firstOfMonth.ToString("MMM yyyy", Ru);
                MonthStats.Add(new StatItem(label, g.Count()));
            }
        }

        /// <summary> List of available years </summary>
        private void UpdateAvailableYears()
        {
            AvailableYears = new SortedSet<int>(Birds.Select(b => b.Arrival.Year));
        }

        /// <summary> Additional metrics (week, day, break) </summary>
        private void CalculateAdditionalMetrics(IList<BirdDTO> filteredBirds)
        {
            if (filteredBirds.Count == 0)
            {
                TopWeek = TopDay = LongestBreak = "—";
                return;
            }

            // The most productive month
            var topMonthGroup = filteredBirds
                .GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (topMonthGroup is not null)
            {
                var firstOfMonth = new DateOnly(topMonthGroup.Key.Year, topMonthGroup.Key.Month, 1);
                var label = firstOfMonth.ToString("MMMM yyyy", Ru);
                TopMonth = $"{label}: {topMonthGroup.Count()} птиц";
            }
            else
            {
                TopMonth = "—";
            }

            // The most productive week
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
                TopWeek = $"{range} — {topWeekGroup.Count()} птиц";
            }
            else
            {
                TopWeek = "—";
            }

            // The most productive day
            var topDayGroup = filteredBirds
                .GroupBy(b => b.Arrival)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            TopDay = topDayGroup != null
                ? $"{topDayGroup.Key:dd.MM.yyyy}: {topDayGroup.Count()} птиц"
                : "—";

            // The most prolong break
            CalculateLongestBreak(filteredBirds);
        }

        /// <summary> Submethod for calculating the most prolong break </summary>
        private void CalculateLongestBreak(IList<BirdDTO> filteredBirds)
        {
            var orderedDates = filteredBirds
                .Select(b => b.Arrival)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (orderedDates.Count <= 1)
            {
                LongestBreak = "—";
                return;
            }

            var maxGap = TimeSpan.Zero;
            DateOnly? start = null, end = null;

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

            LongestBreak = $"{maxGap.TotalDays - 1} дней без поимок (между {start:dd.MM.yyyy} и {end:dd.MM.yyyy})";
        }

        /// <summary> The most prolonged keeping by species </summary>
        private void CalculateLongestKeeping(IList<BirdDTO> filteredBirds)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var byName = filteredBirds.ToLookup(b => b.Name);

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
        }

        /// <summary>
        /// ISO week range formatting helper
        /// </summary>
        private static string FormatIsoWeekRange(int isoYear, int isoWeek)
        {
            var start = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
            var end = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Sunday);

            if (start.Year != end.Year)
                return $"{start:dd MMM yyyy}—{end:dd MMM yyyy}";

            if (start.Month != end.Month)
                return $"{start:dd MMM}-{end:dd MMM yyyy}";

            return $"{start:dd}-{end:dd} {start.ToString("MMMM yyyy", Ru)}";
        }

        #endregion [ Methods ]
    }
}
