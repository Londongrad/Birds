using System.Globalization;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Shared.Localization;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Statistics.Interfaces;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Statistics;

public sealed class BirdStatisticsCalculator(ILocalizationService localization, IBirdNameDisplayService birdNameDisplay)
    : IBirdStatisticsCalculator
{
    private readonly IBirdNameDisplayService _birdNameDisplay = birdNameDisplay;
    private readonly ILocalizationService _localization = localization;

    public BirdStatisticsSnapshot Calculate(IEnumerable<BirdDTO> birds, int? selectedYear)
    {
        var allBirds = birds as IList<BirdDTO> ?? birds.ToList();
        var filteredBirds = FilterBirdsByYear(allBirds, selectedYear);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var speciesStats = BuildSpeciesStats(filteredBirds);
        var yearStats = BuildYearStats(allBirds);
        var monthStats = BuildMonthStats(filteredBirds);
        var longestKeepingStats = BuildLongestKeepingStats(filteredBirds, today);

        return new BirdStatisticsSnapshot
        {
            TotalBirds = filteredBirds.Count,
            AliveCount = allBirds.Count(b => b.IsAlive && b.Departure == null),
            ReleasedCount = filteredBirds.Count(b => b.Departure != null && b.IsAlive),
            KillCount = filteredBirds.Count(b => !b.IsAlive),
            ArrivalsLast30Days = CountArrivalsLast30Days(filteredBirds, today),
            DeparturesLast30Days = CountDeparturesLast30Days(filteredBirds, today),
            PeakConcurrentCount = CalculatePeakConcurrentCount(filteredBirds, today),
            AverageKeeping = FormatAverageKeeping(filteredBirds, today),
            MedianKeeping = FormatMedianKeeping(filteredBirds, today),
            LongestActiveKeeping = FormatLongestActiveKeeping(filteredBirds, today),
            TopMonth = FormatTopMonth(filteredBirds),
            TopWeek = FormatTopWeek(filteredBirds),
            TopDay = FormatTopDay(filteredBirds),
            LongestBreak = FormatLongestBreak(filteredBirds),
            SpeciesStats = speciesStats,
            YearStats = yearStats,
            MonthStats = monthStats,
            LongestKeepingStats = longestKeepingStats,
            TopSpeciesStats = BuildBarItems(speciesStats),
            YearDistributionStats = BuildBarItems(yearStats),
            MonthOfYearStats = BuildMonthOfYearBarItems(filteredBirds, static bird => bird.Arrival.Month),
            DepartureMonthOfYearStats = BuildMonthOfYearBarItems(
                filteredBirds.Where(b => b.Departure.HasValue),
                bird => bird.Departure!.Value.Month),
            AvailableYears = new SortedSet<int>(allBirds.Select(b => b.Arrival.Year))
        };
    }

    private static IList<BirdDTO> FilterBirdsByYear(IList<BirdDTO> birds, int? selectedYear)
    {
        IEnumerable<BirdDTO> query = birds;

        if (selectedYear is int year)
            query = query.Where(b => b.Arrival.Year == year);

        return query as IList<BirdDTO> ?? query.ToList();
    }

    private List<StatItem> BuildSpeciesStats(IList<BirdDTO> filteredBirds)
    {
        return filteredBirds
            .GroupBy(GetDisplayName)
            .Select(group => new StatItem(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToList();
    }

    private List<StatItem> BuildYearStats(IList<BirdDTO> allBirds)
    {
        return allBirds
            .GroupBy(b => b.Arrival.Year)
            .OrderByDescending(group => group.Key)
            .Select(group => new StatItem(group.Key.ToString(CultureInfo.InvariantCulture), group.Count()))
            .ToList();
    }

    private List<StatItem> BuildMonthStats(IList<BirdDTO> filteredBirds)
    {
        return filteredBirds
            .GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
            .OrderByDescending(group => group.Key.Year)
            .ThenByDescending(group => group.Key.Month)
            .Select(group =>
            {
                var firstOfMonth = new DateOnly(group.Key.Year, group.Key.Month, 1);
                var label = _localization.FormatDate(firstOfMonth, DateDisplayStyle.MonthYearShort);
                return new StatItem(label, group.Count());
            })
            .ToList();
    }

    private List<StatItem> BuildLongestKeepingStats(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        return filteredBirds
            .GroupBy(GetDisplayName)
            .Select(group => new StatItem(
                group.Key,
                group.Select(b => CalculateKeepingDays(b, today)).DefaultIfEmpty(0).Max()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToList();
    }

    private List<StatBarItem> BuildMonthOfYearBarItems(IEnumerable<BirdDTO> birds, Func<BirdDTO, int> monthSelector)
    {
        var countsByMonth = birds
            .GroupBy(monthSelector)
            .ToDictionary(group => group.Key, group => group.Count());

        var items = Enumerable.Range(1, 12)
            .Select(month => new StatItem(GetMonthLabel(month), countsByMonth.GetValueOrDefault(month)))
            .ToList();

        return BuildBarItems(items);
    }

    private static List<StatBarItem> BuildBarItems(IEnumerable<StatItem> items)
    {
        var itemList = items.ToList();
        var maxCount = itemList.Count > 0 ? itemList.Max(x => x.Count) : 0;

        return itemList
            .Select(item => new StatBarItem(
                item.Label,
                item.Count,
                maxCount <= 0 ? 0d : (double)item.Count / maxCount))
            .ToList();
    }

    private string FormatAverageKeeping(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        if (filteredBirds.Count == 0)
            return "\u2014";

        var averageKeepingDays = filteredBirds
            .Select(b => CalculateKeepingDays(b, today))
            .DefaultIfEmpty(0)
            .Average();

        return _localization.GetString(
            "Statistics.AverageKeepingValue",
            (int)Math.Round(averageKeepingDays, MidpointRounding.AwayFromZero));
    }

    private string FormatTopMonth(IList<BirdDTO> filteredBirds)
    {
        var topMonthGroup = filteredBirds
            .GroupBy(b => new { b.Arrival.Year, b.Arrival.Month })
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        if (topMonthGroup is null)
            return "\u2014";

        var firstOfMonth = new DateOnly(topMonthGroup.Key.Year, topMonthGroup.Key.Month, 1);
        var label = _localization.FormatDate(firstOfMonth, DateDisplayStyle.MonthYearLong);
        return $"{label} \u2013 {AppText.Format("Statistics.CountBirds", topMonthGroup.Count())}";
    }

    private string FormatTopWeek(IList<BirdDTO> filteredBirds)
    {
        var topWeekGroup = filteredBirds
            .GroupBy(b =>
            {
                var dateTime = b.Arrival.ToDateTime(TimeOnly.MinValue);
                return new
                {
                    Year = ISOWeek.GetYear(dateTime),
                    Week = ISOWeek.GetWeekOfYear(dateTime)
                };
            })
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        if (topWeekGroup is null)
            return "\u2014";

        return
            $"{FormatIsoWeekRange(topWeekGroup.Key.Year, topWeekGroup.Key.Week)} \u2013 {AppText.Format("Statistics.CountBirds", topWeekGroup.Count())}";
    }

    private string FormatTopDay(IList<BirdDTO> filteredBirds)
    {
        var topDayGroup = filteredBirds
            .GroupBy(b => b.Arrival)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        return topDayGroup is null
            ? "\u2014"
            : $"{_localization.FormatDate(topDayGroup.Key, DateDisplayStyle.Long)} \u2013 {AppText.Format("Statistics.CountBirds", topDayGroup.Count())}";
    }

    private string FormatLongestBreak(IList<BirdDTO> filteredBirds)
    {
        var orderedDates = filteredBirds
            .Select(b => b.Arrival)
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        if (orderedDates.Count <= 1)
            return "\u2014";

        var maxGap = TimeSpan.Zero;
        DateOnly? start = null;
        DateOnly? end = null;

        for (var i = 1; i < orderedDates.Count; i++)
        {
            var gap = orderedDates[i].ToDateTime(TimeOnly.MinValue) -
                      orderedDates[i - 1].ToDateTime(TimeOnly.MinValue);

            if (gap <= maxGap)
                continue;

            maxGap = gap;
            start = orderedDates[i - 1];
            end = orderedDates[i];
        }

        return AppText.Format(
            _localization.CurrentCulture,
            "Statistics.LongestBreakValue",
            maxGap.TotalDays - 1,
            _localization.FormatDate(start),
            _localization.FormatDate(end));
    }

    private static int CountArrivalsLast30Days(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        var since = today.AddDays(-29);
        return filteredBirds.Count(b => b.Arrival >= since && b.Arrival <= today);
    }

    private static int CountDeparturesLast30Days(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        var since = today.AddDays(-29);
        return filteredBirds.Count(b => b.Departure is { } departure && departure >= since && departure <= today);
    }

    private static int CalculatePeakConcurrentCount(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        if (filteredBirds.Count == 0)
            return 0;

        var changes = new SortedDictionary<DateOnly, int>();

        foreach (var bird in filteredBirds)
        {
            AddChange(changes, bird.Arrival, 1);
            AddChange(changes, (bird.Departure ?? today).AddDays(1), -1);
        }

        var running = 0;
        var peak = 0;

        foreach (var change in changes.OrderBy(x => x.Key))
        {
            running += change.Value;
            peak = Math.Max(peak, running);
        }

        return peak;
    }

    private string FormatMedianKeeping(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        if (filteredBirds.Count == 0)
            return "\u2014";

        var durations = filteredBirds
            .Select(b => CalculateKeepingDays(b, today))
            .OrderBy(x => x)
            .ToArray();

        var median = durations.Length % 2 == 1
            ? durations[durations.Length / 2]
            : (durations[durations.Length / 2 - 1] + durations[durations.Length / 2]) / 2d;

        return _localization.GetString(
            "Statistics.MedianKeepingValue",
            (int)Math.Round(median, MidpointRounding.AwayFromZero));
    }

    private string FormatLongestActiveKeeping(IList<BirdDTO> filteredBirds, DateOnly today)
    {
        var longestActiveBird = filteredBirds
            .Where(b => b.IsAlive && b.Departure is null)
            .OrderBy(b => b.Arrival)
            .FirstOrDefault();

        if (longestActiveBird is null)
            return "\u2014";

        var days = CalculateKeepingDays(longestActiveBird, today);
        var displayName = GetDisplayName(longestActiveBird);

        return AppText.Format(
            _localization.CurrentCulture,
            "Statistics.LongestActiveKeepingValue",
            displayName,
            days,
            _localization.FormatDate(longestActiveBird.Arrival));
    }

    private static int CalculateKeepingDays(BirdDTO bird, DateOnly today)
    {
        var end = bird.Departure ?? today;
        return (int)Math.Max(
            0,
            (end.ToDateTime(TimeOnly.MinValue) - bird.Arrival.ToDateTime(TimeOnly.MinValue)).TotalDays);
    }

    private string GetDisplayName(BirdDTO bird)
    {
        return BirdEnumHelper.ParseBirdName(bird.Name) is { } species
            ? _birdNameDisplay.GetDisplayName(species)
            : bird.Name;
    }

    private string FormatIsoWeekRange(int isoYear, int isoWeek)
    {
        var start = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
        var end = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Sunday));
        return
            $"{_localization.FormatDate(start, DateDisplayStyle.Medium)} \u2013 {_localization.FormatDate(end, DateDisplayStyle.Medium)}";
    }

    private string GetMonthLabel(int month)
    {
        var label = _localization.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
        return string.IsNullOrWhiteSpace(label)
            ? month.ToString(CultureInfo.InvariantCulture)
            : label;
    }

    private static void AddChange(IDictionary<DateOnly, int> changes, DateOnly day, int delta)
    {
        if (!changes.TryAdd(day, delta))
            changes[day] += delta;
    }
}
