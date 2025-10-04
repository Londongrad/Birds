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
        public ObservableCollection<BirdDTO> Birds { get; }

        // Top metric cards (recomputed with filter applied)
        [ObservableProperty] private int totalBirds;
        [ObservableProperty] private int releasedCount;
        [ObservableProperty] private int killCount;

        // Filter state
        [ObservableProperty] private int? selectedYear;
        [ObservableProperty] private IReadOnlyList<int> availableYears = Array.Empty<int>();

        // UI lists
        public ObservableCollection<StatItem> SpeciesStats { get; } = new();
        public ObservableCollection<StatItem> YearStats { get; } = new();
        public ObservableCollection<StatItem> MonthStats { get; } = new();

        public BirdStatisticsViewModel(IBirdStore birdStore)
        {
            Birds = birdStore.Birds;

            // React on collection changes
            Birds.CollectionChanged += OnBirdsChanged;

            // Initial fill
            Recalculate();
        }

        private void OnBirdsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Recalculate();
        }

        partial void OnSelectedYearChanged(int? value) => Recalculate();

        [RelayCommand]
        private void ClearYearFilter() => SelectedYear = null;

        private void Recalculate()
        {
            IEnumerable<BirdDTO> q = Birds;
            if (SelectedYear is int y)
                q = q.Where(b => b.Arrival.Year == y);

            // Metrics
            TotalBirds = q.Count();
            ReleasedCount = q.Count(b => b.Departure != null && b.IsAlive == true);
            KillCount = q.Where(b => b.IsAlive == false).Count();

            // Species stats (filtered)
            SpeciesStats.Clear();
            foreach (var g in q.GroupBy(b => b.Name)
                               .OrderByDescending(g => g.Count()))
            {
                SpeciesStats.Add(new StatItem(g.Key, g.Count()));
            }

            // Year stats (always for all years to show global picture)
            YearStats.Clear();
            foreach (var g in Birds.GroupBy(b => b.Arrival.Year).OrderBy(g => g.Key))
            {
                YearStats.Add(new StatItem(g.Key.ToString(), g.Count()));
            }

            // Month stats (respect filter)
            MonthStats.Clear();
            foreach (var g in q.GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
                               .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month))
            {
                var label = $"{g.Key.Year}-{g.Key.Month:00}";
                MonthStats.Add(new StatItem(label, g.Count()));
            }

            // Year filter choices
            AvailableYears = Birds.Select(b => b.Arrival.Year)
                                  .Distinct()
                                  .OrderBy(y => y)
                                  .ToList();
        }
    }
}
