using System.Globalization;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Shared.Localization;
using Birds.Tests.UI.Services;
using Birds.UI.Enums;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.ViewModels;

public class BirdListViewModelTests
{
    [Fact]
    public void Filters_Should_Contain_Default_Options_And_All_Bird_Species()
    {
        var sut = CreateViewModel();

        sut.Filters.Should().HaveCount(4 + Enum.GetValues<BirdsName>().Length);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.All);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.Alive);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.Dead);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.DepartedButAlive);
        sut.Filters.Count(x => x.Filter == BirdFilter.BySpecies).Should().Be(Enum.GetValues<BirdsName>().Length);
    }

    [Fact]
    public void FilterBirds_Should_Filter_By_Selected_Species_Without_String_Switches()
    {
        var sparrow = CreateBird((BirdsName)1);
        var chickadee = CreateBird((BirdsName)6);
        var sut = CreateViewModel(sparrow, chickadee);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)6);

        sut.FilterBirds(sparrow).Should().BeFalse();
        sut.FilterBirds(chickadee).Should().BeTrue();
    }

    [Fact]
    public void FilterBirds_Should_Exclude_Invalid_Bird_Name_When_Filtering_By_Species()
    {
        var invalidBird = TestHelpers.Bird(name: "Unknown bird");
        var sut = CreateViewModel(invalidBird);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)1);

        sut.FilterBirds(invalidBird).Should().BeFalse();
    }

    [Fact]
    public void FilterBirds_Should_Combine_Search_And_Species_Filter()
    {
        var sparrow = CreateBird((BirdsName)1, "forest visitor");
        var secondSparrow = CreateBird((BirdsName)1, "city bird");
        var sut = CreateViewModel(sparrow, secondSparrow);
        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)1);
        sut.SearchText = "forest";

        sut.FilterBirds(sparrow).Should().BeTrue();
        sut.FilterBirds(secondSparrow).Should().BeFalse();
    }

    [Fact]
    public void BirdCount_Should_Track_Filtered_View()
    {
        var sparrow = CreateBird((BirdsName)1, "forest visitor");
        var chickadee = CreateBird((BirdsName)6, "city bird");
        var sut = CreateViewModel(sparrow, chickadee);

        sut.BirdCount.Should().Be(2);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdsName)6);
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "city";
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "forest";
        sut.BirdCount.Should().Be(0);
    }

    [Fact]
    public void Search_Should_Match_Configured_Date_Format()
    {
        var bird = CreateBird((BirdsName)1);
        var sut = CreateViewModel(DateDisplayFormats.YearMonthDay, bird);
        sut.SearchText = "2026-04";

        sut.FilterBirds(bird).Should().BeTrue();
    }

    private static BirdListViewModel CreateViewModel(params BirdDTO[] birds)
    {
        return CreateViewModel(DateDisplayFormats.DayMonthYear, birds);
    }

    private static BirdListViewModel CreateViewModel(string dateFormat, params BirdDTO[] birds)
    {
        var store = new BirdStore();
        store.CompleteLoading();

        foreach (var bird in birds)
            store.Birds.Add(bird);

        var manager = new Mock<IBirdManager>();
        manager.SetupGet(x => x.Store).Returns(store);

        var localization = new Mock<ILocalizationService>();
        var culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        localization.SetupGet(x => x.CurrentCulture).Returns(culture);
        localization.SetupGet(x => x.CurrentDateFormat).Returns(dateFormat);
        localization.Setup(x => x.FormatDate(It.IsAny<DateOnly>(), It.IsAny<DateDisplayStyle>()))
            .Returns((DateOnly value, DateDisplayStyle style) =>
                DateDisplayFormats.FormatDate(value, culture, dateFormat, style));
        localization.Setup(x => x.FormatDate(It.IsAny<DateOnly?>(), It.IsAny<DateDisplayStyle>(), It.IsAny<string?>()))
            .Returns((DateOnly? value, DateDisplayStyle style, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDate(value.Value, culture, dateFormat, style)
                    : fallback ?? "\u2014");

        return new BirdListViewModel(manager.Object, localization.Object);
    }

    private static BirdDTO CreateBird(BirdsName species, string? desc = null)
    {
        return TestHelpers.Bird(name: species.ToDisplayName(), desc: desc);
    }
}