using System.Globalization;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.Tests.UI.Services;
using Birds.UI.Enums;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Caching;
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

        sut.Filters.Should().HaveCount(4 + Enum.GetValues<BirdSpecies>().Length);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.All);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.Alive);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.Dead);
        sut.Filters.Should().ContainSingle(x => x.Filter == BirdFilter.DepartedButAlive);
        sut.Filters.Count(x => x.Filter == BirdFilter.BySpecies).Should().Be(Enum.GetValues<BirdSpecies>().Length);
    }

    [Fact]
    public void FilterBirds_Should_Filter_By_Selected_Species_Without_String_Switches()
    {
        var sparrow = CreateBird((BirdSpecies)1);
        var chickadee = CreateBird((BirdSpecies)6);
        var sut = CreateViewModel(sparrow, chickadee);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdSpecies)6);

        sut.FilterBirds(sparrow).Should().BeFalse();
        sut.FilterBirds(chickadee).Should().BeTrue();
    }

    [Fact]
    public void FilterBirds_Should_Exclude_Invalid_Bird_Name_When_Filtering_By_Species()
    {
        var invalidBird = TestHelpers.Bird(name: "Unknown bird");
        var sut = CreateViewModel(invalidBird);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdSpecies)1);

        sut.FilterBirds(invalidBird).Should().BeFalse();
    }

    [Fact]
    public void FilterBirds_Should_Combine_Search_And_Species_Filter()
    {
        var sparrow = CreateBird((BirdSpecies)1, "forest visitor");
        var secondSparrow = CreateBird((BirdSpecies)1, "city bird");
        var sut = CreateViewModel(sparrow, secondSparrow);
        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdSpecies)1);
        sut.SearchText = "forest";

        sut.FilterBirds(sparrow).Should().BeTrue();
        sut.FilterBirds(secondSparrow).Should().BeFalse();
    }

    [Fact]
    public void BirdCount_Should_Track_Filtered_View()
    {
        var sparrow = CreateBird((BirdSpecies)1, "forest visitor");
        var chickadee = CreateBird((BirdSpecies)6, "city bird");
        var sut = CreateViewModel(sparrow, chickadee);

        sut.BirdCount.Should().Be(2);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdSpecies)6);
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "city";
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "forest";
        sut.BirdCount.Should().Be(0);
    }

    [Fact]
    public void Search_Should_Match_Configured_Date_Format()
    {
        var bird = CreateBird((BirdSpecies)1);
        var sut = CreateViewModel(DateDisplayFormats.YearMonthDay, bird);
        sut.SearchText = "2026-04";

        sut.FilterBirds(bird).Should().BeTrue();
    }

    [Fact]
    public void Ctor_Should_Not_Create_Item_ViewModels_For_Dtos()
    {
        var birds = Enumerable.Range(0, 50)
            .Select(_ => CreateBird((BirdSpecies)1))
            .ToArray();

        _ = CreateViewModelWithCache(out var cache, DateDisplayFormats.DayMonthYear, birds);

        cache.Verify(x => x.GetOrCreate(It.IsAny<BirdDTO>()), Times.Never);
    }

    [Fact]
    public void BirdsCollectionChanged_When_BirdRemoved_Should_Remove_Cached_ViewModel()
    {
        var bird = CreateBird((BirdSpecies)1);
        var sut = CreateViewModelWithCache(out var cache, DateDisplayFormats.DayMonthYear, bird);

        sut.Birds.Remove(bird);

        cache.Verify(x => x.Remove(bird.Id), Times.Once);
    }

    [Fact]
    public void BirdsCollectionChanged_When_BirdReplaced_Should_Refresh_Cached_ViewModel()
    {
        var bird = CreateBird((BirdSpecies)1);
        var updated = bird with { Description = "updated" };
        var sut = CreateViewModelWithCache(out var cache, DateDisplayFormats.DayMonthYear, bird);

        sut.Birds[0] = updated;

        cache.Verify(x => x.Refresh(updated), Times.Once);
        cache.Verify(x => x.Remove(bird.Id), Times.Never);
    }

    [Fact]
    public void BirdsCollectionChanged_When_BirdsReset_Should_Clear_Cache()
    {
        var bird = CreateBird((BirdSpecies)1);
        var sut = CreateViewModelWithCache(out var cache, DateDisplayFormats.DayMonthYear, bird);

        sut.Birds.Clear();

        cache.Verify(x => x.Clear(), Times.Once);
    }

    [Fact]
    public void Dispose_Should_Dispose_BirdViewModel_Cache()
    {
        var sut = CreateViewModelWithCache(out var cache, DateDisplayFormats.DayMonthYear);

        sut.Dispose();

        cache.Verify(x => x.Dispose(), Times.Once);
    }

    private static BirdListViewModel CreateViewModel(params BirdDTO[] birds)
    {
        return CreateViewModel(DateDisplayFormats.DayMonthYear, birds);
    }

    private static BirdListViewModel CreateViewModel(string dateFormat, params BirdDTO[] birds)
    {
        return CreateViewModelWithCache(out _, dateFormat, birds);
    }

    private static BirdListViewModel CreateViewModelWithCache(
        out Mock<IBirdViewModelCache> cache,
        string dateFormat,
        params BirdDTO[] birds)
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

        var birdNameDisplay = new BirdNameDisplayService(localization.Object);
        cache = new Mock<IBirdViewModelCache>();

        return new BirdListViewModel(manager.Object, localization.Object, birdNameDisplay, cache.Object);
    }

    private static BirdDTO CreateBird(BirdSpecies species, string? desc = null)
    {
        return TestHelpers.Bird(name: BirdNameDisplayNames.GetDisplayName(species), desc: desc);
    }
}
