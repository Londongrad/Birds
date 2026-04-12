using Birds.UI.ViewModels;

namespace Birds.UI.Services.Statistics;

public sealed class BirdStatisticsSnapshot
{
    public required int TotalBirds { get; init; }
    public required int AliveCount { get; init; }
    public required int ReleasedCount { get; init; }
    public required int KillCount { get; init; }
    public required int ArrivalsLast30Days { get; init; }
    public required int DeparturesLast30Days { get; init; }
    public required int PeakConcurrentCount { get; init; }
    public required string AverageKeeping { get; init; }
    public required string MedianKeeping { get; init; }
    public required string LongestActiveKeeping { get; init; }
    public required string TopMonth { get; init; }
    public required string TopWeek { get; init; }
    public required string TopDay { get; init; }
    public required string LongestBreak { get; init; }
    public required IReadOnlyList<StatItem> SpeciesStats { get; init; }
    public required IReadOnlyList<StatItem> YearStats { get; init; }
    public required IReadOnlyList<StatItem> MonthStats { get; init; }
    public required IReadOnlyList<StatItem> LongestKeepingStats { get; init; }
    public required IReadOnlyList<StatBarItem> TopSpeciesStats { get; init; }
    public required IReadOnlyList<StatBarItem> YearDistributionStats { get; init; }
    public required IReadOnlyList<StatBarItem> MonthOfYearStats { get; init; }
    public required IReadOnlyList<StatBarItem> DepartureMonthOfYearStats { get; init; }
    public required IReadOnlyCollection<int> AvailableYears { get; init; }
}
