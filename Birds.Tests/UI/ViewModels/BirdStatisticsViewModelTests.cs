using System.Globalization;
using Birds.Application.DTOs;
using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.ViewModels;

public class BirdStatisticsViewModelTests
{
    [Fact]
    public void Summary_Should_Track_Alive_Released_And_Dead_Birds()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sut = CreateViewModel(
            AppLanguages.English,
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, today.AddDays(-4), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 14), true,
                null, null),
            new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 5), false, null,
                null));

        sut.TotalBirds.Should().Be(3);
        sut.AliveCount.Should().Be(1);
        sut.ReleasedCount.Should().Be(1);
        sut.KillCount.Should().Be(1);
        sut.HasBirds.Should().BeTrue();
        sut.SpeciesStats.Should().HaveCount(2);
        sut.SpeciesStats.Should().ContainSingle(x => x.Count == 2);
        sut.AverageKeeping.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SelectedYear_Should_Filter_Current_Scope_And_Localize_Summary()
    {
        var sut = CreateViewModel(
            AppLanguages.Russian,
            new BirdDTO(Guid.NewGuid(), "Воробей", null, new DateOnly(2025, 3, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Амадин", null, new DateOnly(2026, 4, 11), new DateOnly(2026, 4, 16), true,
                null, null));

        sut.FilterSummary.Should().Contain("архиву");

        sut.SelectedYear = 2025;

        sut.TotalBirds.Should().Be(1);
        sut.AliveCount.Should().Be(1);
        sut.SpeciesStats.Should().HaveCount(1);
        sut.SpeciesStats[0].Count.Should().Be(1);
        sut.FilterSummary.Should().Contain("2025");
        sut.YearStats.Should().HaveCount(2);
    }

    [Fact]
    public void AliveCount_Should_Ignore_SelectedYear_Filter()
    {
        var sut = CreateViewModel(
            AppLanguages.English,
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2025, 3, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Goldfinch", null, new DateOnly(2026, 4, 11), new DateOnly(2026, 4, 16), true,
                null, null),
            new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2026, 4, 12), new DateOnly(2026, 4, 18), false,
                null, null));

        sut.SelectedYear = 2026;

        sut.TotalBirds.Should().Be(2);
        sut.AliveCount.Should().Be(1);
    }

    [Fact]
    public void Distribution_Should_Build_Top_Species_And_Year_Bars()
    {
        var sut = CreateViewModel(
            AppLanguages.English,
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 1, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 1, 11), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Goldfinch", null, new DateOnly(2025, 2, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2025, 3, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Great tit", null, new DateOnly(2024, 4, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Nuthatch", null, new DateOnly(2024, 5, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Chickadee", null, new DateOnly(2024, 6, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Hawfinch", null, new DateOnly(2024, 7, 10), null, true, null, null));

        sut.TopSpeciesStats.Should().HaveCount(7);
        sut.TopSpeciesStats.First().Count.Should().Be(2);
        sut.TopSpeciesStats.First().Ratio.Should().Be(1d);
        sut.YearDistributionStats.Should().Contain(x => x.Label == "2026" && x.Count == 2);
    }

    [Fact]
    public void MonthOfYear_Distribution_Should_Expose_All_Twelve_Months()
    {
        var sut = CreateViewModel(
            AppLanguages.Russian,
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 1, 10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Goldfinch", null, new DateOnly(2026, 3, 12), null, true, null, null));

        sut.MonthOfYearStats.Should().HaveCount(12);
        sut.MonthOfYearStats.ElementAt(0).Count.Should().Be(1);
        sut.MonthOfYearStats.ElementAt(1).Count.Should().Be(0);
        sut.MonthOfYearStats.ElementAt(2).Count.Should().Be(1);
    }

    [Fact]
    public void Overview_Metrics_Should_Expose_Rolling_Peak_And_Median_Data()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sut = CreateViewModel(
            AppLanguages.English,
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, today.AddDays(-10), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Goldfinch", null, today.AddDays(-20), today.AddDays(-5), true, null, null),
            new BirdDTO(Guid.NewGuid(), "Amadin", null, today.AddDays(-40), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Great tit", null, today.AddDays(-100), today.AddDays(-50), false, null, null));

        sut.ArrivalsLast30Days.Should().Be(2);
        sut.DeparturesLast30Days.Should().Be(1);
        sut.PeakConcurrentCount.Should().Be(3);
        sut.MedianKeeping.Should().Be("Median of 28 days");
        sut.LongestActiveKeeping.Should().Contain("40");
        sut.LongestActiveKeeping.Should().Contain(today.AddDays(-40).ToString("dd.MM.yyyy"));
    }

    [Fact]
    public void DepartureMonth_Distribution_Should_Expose_All_Twelve_Months()
    {
        var sut = CreateViewModel(
            AppLanguages.English,
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 15), true,
                null, null),
            new BirdDTO(Guid.NewGuid(), "Goldfinch", null, new DateOnly(2026, 3, 12), new DateOnly(2026, 3, 20), true,
                null, null),
            new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2026, 3, 18), null, true, null, null));

        sut.DepartureMonthOfYearStats.Should().HaveCount(12);
        sut.DepartureMonthOfYearStats.ElementAt(0).Count.Should().Be(1);
        sut.DepartureMonthOfYearStats.ElementAt(1).Count.Should().Be(0);
        sut.DepartureMonthOfYearStats.ElementAt(2).Count.Should().Be(1);
    }

    private static BirdStatisticsViewModel CreateViewModel(string language, params BirdDTO[] birds)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(x => x.CurrentCulture).Returns(culture);
        localization.Setup(x => x.GetString(It.IsAny<string>()))
            .Returns((string key) => AppText.Get(key, culture));
        localization.Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => AppText.Format(culture, key, args));
        localization.Setup(x => x.FormatDate(It.IsAny<DateOnly>(), It.IsAny<DateDisplayStyle>()))
            .Returns((DateOnly value, DateDisplayStyle style) =>
                DateDisplayFormats.FormatDate(value, culture, DateDisplayFormats.DayMonthYear, style));
        localization.Setup(x => x.FormatDate(It.IsAny<DateOnly?>(), It.IsAny<DateDisplayStyle>(), It.IsAny<string?>()))
            .Returns((DateOnly? value, DateDisplayStyle style, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDate(value.Value, culture, DateDisplayFormats.DayMonthYear, style)
                    : fallback ?? "\u2014");

        var store = new BirdStore();
        store.CompleteLoading();

        foreach (var bird in birds)
            store.Birds.Add(bird);

        return new BirdStatisticsViewModel(store, localization.Object);
    }
}