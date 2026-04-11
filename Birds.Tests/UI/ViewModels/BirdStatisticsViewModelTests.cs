using Birds.Application.DTOs;
using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;
using System.Globalization;

namespace Birds.Tests.UI.ViewModels
{
    public class BirdStatisticsViewModelTests
    {
        [Fact]
        public void Summary_Should_Track_Alive_Released_And_Dead_Birds()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var sut = CreateViewModel(
                AppLanguages.English,
                new BirdDTO(Guid.NewGuid(), "Sparrow", null, today.AddDays(-4), null, true, null, null),
                new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 14), true, null, null),
                new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 5), false, null, null));

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
                new BirdDTO(Guid.NewGuid(), "Амадин", null, new DateOnly(2026, 4, 11), new DateOnly(2026, 4, 16), true, null, null));

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
                new BirdDTO(Guid.NewGuid(), "Goldfinch", null, new DateOnly(2026, 4, 11), new DateOnly(2026, 4, 16), true, null, null),
                new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2026, 4, 12), new DateOnly(2026, 4, 18), false, null, null));

            sut.SelectedYear = 2026;

            sut.TotalBirds.Should().Be(2);
            sut.AliveCount.Should().Be(1);
        }

        [Fact]
        public void Highlights_Should_Use_Short_Dashes()
        {
            var sut = CreateViewModel(
                AppLanguages.English,
                new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 10, 30), null, true, null, null),
                new BirdDTO(Guid.NewGuid(), "Goldfinch", null, new DateOnly(2026, 10, 30), null, true, null, null),
                new BirdDTO(Guid.NewGuid(), "Amadin", null, new DateOnly(2026, 10, 31), null, true, null, null));

            sut.TopDay.Should().Contain(" \u2013 ");
            sut.TopMonth.Should().Contain(" \u2013 ");
            sut.TopWeek.Should().Contain(" \u2013 ");
            sut.TopDay.Should().NotContain(" \u2014 ");
            sut.TopMonth.Should().NotContain(" \u2014 ");
            sut.TopWeek.Should().NotContain(" \u2014 ");
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
                .Returns((DateOnly value, DateDisplayStyle style) => DateDisplayFormats.FormatDate(value, culture, DateDisplayFormats.DayMonthYear, style));
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
}
