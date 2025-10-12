using Birds.Application.DTOs;
using Birds.UI.Services.Stores.BirdStore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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

        public BirdStatisticsViewModel(IBirdStore birdStore)
        {
            Birds = birdStore.Birds;

            // React on collection changes
            Birds.CollectionChanged += OnBirdsChanged;

            // Initial fill
            Recalculate();
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
            // 1️) Фильтрация
            IEnumerable<BirdDTO> q = Birds;
            if (SelectedYear is int y)
                q = q.Where(b => b.Arrival.Year == y);

            var filteredBirds = q as IList<BirdDTO> ?? q.ToList();

            // 2️) Быстрый подсчёт карточек (в один проход)
            int total = 0, released = 0, killed = 0;
            foreach (var b in filteredBirds)
            {
                total++;
                if (b.Departure != null && b.IsAlive == true)
                    released++;
                else if (b.IsAlive == false)
                    killed++;
            }

            TotalBirds = total;
            ReleasedCount = released;
            KillCount = killed;

            // 3️) Группировка по виду (одна для Species + LongestKeeping). Лучше, чем в нескольких местах GroupBy.
            var byName = filteredBirds.ToLookup(b => b.Name);

            // Статистика по виду птицы
            SpeciesStats.Clear();
            foreach (var group in byName.OrderByDescending(g => g.Count()))
                SpeciesStats.Add(new StatItem(group.Key, group.Count()));

            // 4️) Группировка по году (всегда глобальная)
            YearStats.Clear();
            foreach (var g in Birds.GroupBy(b => b.Arrival.Year).OrderBy(g => g.Key))
                YearStats.Add(new StatItem(g.Key.ToString(), g.Count()));

            // 5️) Группировка по месяцам (в рамках фильтра)
            MonthStats.Clear();
            foreach (var g in filteredBirds
                .GroupBy(b => b.Arrival.ToString("yyyy-MM"))
                .OrderBy(g => g.Key))
            {
                MonthStats.Add(new StatItem(g.Key, g.Count()));
            }

            // 6️) Список лет (для фильтра)
            AvailableYears = new SortedSet<int>(Birds.Select(b => b.Arrival.Year));

            // 7️) Дополнительные метрики
            if (filteredBirds.Count > 0)
            {
                // Самая продуктивная неделя
                var topWeekGroup = filteredBirds
                    .GroupBy(b =>
                    {
                        var dt = b.Arrival.ToDateTime(TimeOnly.MinValue);
                        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                            dt,
                            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                    })
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                TopWeek = topWeekGroup != null
                    ? $"Неделя {topWeekGroup.Key}: {topWeekGroup.Count()} птиц"
                    : "—";

                // Самый продуктивный день
                var topDayGroup = filteredBirds
                    .GroupBy(b => b.Arrival)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                TopDay = topDayGroup != null
                    ? $"{topDayGroup.Key:dd.MM.yyyy}: {topDayGroup.Count()} птиц"
                    : "—";

                // Самый длинный перерыв
                var orderedDates = filteredBirds
                    .Select(b => b.Arrival)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                if (orderedDates.Count > 1)
                {
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
                else
                {
                    LongestBreak = "—";
                }
            }
            else
            {
                TopWeek = TopDay = LongestBreak = "—";
            }

            // 8️) Самое долгое содержание по виду
            LongestKeepingStats.Clear();
            var today = DateOnly.FromDateTime(DateTime.Today);

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
    }
}
