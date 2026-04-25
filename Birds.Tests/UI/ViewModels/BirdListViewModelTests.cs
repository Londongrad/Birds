using System.Collections.Specialized;
using System.Globalization;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.Tests.Helpers;
using Birds.Tests.UI.Services;
using Birds.UI.Enums;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Caching;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Search;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.ViewModels;

public class BirdListViewModelTests
{
    private static readonly TimeSpan SearchDebounceDelay = TimeSpan.FromMilliseconds(30);

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
    public async Task BirdCount_Should_Track_Filtered_View()
    {
        var sparrow = CreateBird((BirdSpecies)1, "forest visitor");
        var chickadee = CreateBird((BirdSpecies)6, "city bird");
        var sut = CreateViewModel(sparrow, chickadee);

        sut.BirdCount.Should().Be(2);

        sut.SelectedFilter = sut.Filters.Single(x => x.Filter == BirdFilter.BySpecies && x.Species == (BirdSpecies)6);
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "city";
        await WaitUntilAsync(() => sut.BirdCount == 1);
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "forest";
        await WaitUntilAsync(() => sut.BirdCount == 0);
        sut.BirdCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchTextChanged_Should_Debounce_Filter_Refresh()
    {
        var sparrow = CreateBird((BirdSpecies)1, "forest visitor");
        var chickadee = CreateBird((BirdSpecies)6, "city bird");
        var sut = CreateViewModel(sparrow, chickadee);

        sut.SearchText = "forest";

        sut.BirdCount.Should().Be(2);
        await WaitUntilAsync(() => sut.BirdCount == 1);
    }

    [Fact]
    public async Task SearchTextChanged_WhenTypedRapidly_Should_Refresh_Only_Final_Search()
    {
        var sparrow = CreateBird((BirdSpecies)1, "forest visitor");
        var chickadee = CreateBird((BirdSpecies)6, "city bird");
        var sut = CreateViewModel(sparrow, chickadee);
        var refreshCount = 0;
        ((INotifyCollectionChanged)sut.BirdsView).CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                refreshCount++;
        };

        sut.SearchText = "f";
        sut.SearchText = "fo";
        sut.SearchText = "forest";

        refreshCount.Should().Be(0);
        await WaitUntilAsync(() => refreshCount == 1);
        await Task.Delay(SearchDebounceDelay * 3);

        refreshCount.Should().Be(1);
        sut.BirdCount.Should().Be(1);
    }

    [Fact]
    public async Task ClearSearch_Should_Restore_All_Birds()
    {
        var sparrow = CreateBird((BirdSpecies)1, "forest visitor");
        var chickadee = CreateBird((BirdSpecies)6, "city bird");
        var sut = CreateViewModel(sparrow, chickadee);

        sut.SearchText = "city";
        await WaitUntilAsync(() => sut.BirdCount == 1);

        sut.ClearSearchCommand.Execute(null);
        await WaitUntilAsync(() => sut.BirdCount == 2);

        sut.BirdCount.Should().Be(2);
    }

    [Fact]
    public async Task Search_Should_Match_DisplayName_Description_And_Dates()
    {
        var bird = CreateBird((BirdSpecies)1, "forest visitor");
        var sut = CreateViewModel(DateDisplayFormats.YearMonthDay, bird);

        sut.SearchText = BirdNameDisplayNames.GetDisplayName((BirdSpecies)1);
        await WaitUntilAsync(() => sut.BirdCount == 1);
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "forest";
        await WaitUntilAsync(() => sut.BirdCount == 1);
        sut.BirdCount.Should().Be(1);

        sut.SearchText = "2026-04";
        await WaitUntilAsync(() => sut.BirdCount == 1);
        sut.BirdCount.Should().Be(1);
    }

    [Fact]
    public async Task LanguageChanged_Should_Refresh_Search_When_Localized_DisplayNames_Change()
    {
        var culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        var bird = TestHelpers.Bird(name: "legacy", desc: "bird") with
        {
            Species = BirdSpecies.Sparrow
        };
        var sut = CreateViewModelWithCulture(
            out var localization,
            () => culture,
            DateDisplayFormats.DayMonthYear,
            bird);

        sut.SearchText = "Sparrow";
        await WaitUntilAsync(() => sut.BirdCount == 0);

        culture = CultureInfo.GetCultureInfo(AppLanguages.English);
        localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.BirdCount.Should().Be(1);
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
    public async Task Search_Should_Not_Create_Item_ViewModels_For_Dtos()
    {
        var birds = Enumerable.Range(0, 50)
            .Select(_ => CreateBird((BirdSpecies)1))
            .ToArray();

        var sut = CreateViewModelWithCache(out var cache, DateDisplayFormats.DayMonthYear, birds);

        sut.SearchText = "not-present";
        await WaitUntilAsync(() => sut.BirdCount == 0);

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

    [Fact]
    public async Task ReloadBirdsCommand_Should_Pass_Cancelable_Token_To_Manager()
    {
        CancellationToken capturedToken = default;
        var sut = CreateViewModelWithManager(
            out var manager,
            out _,
            DateDisplayFormats.DayMonthYear);
        manager.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedToken = token)
            .Returns(Task.CompletedTask);

        await sut.ReloadBirdsCommand.ExecuteAsync(null);

        capturedToken.CanBeCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_Should_Cancel_Running_Reload()
    {
        var reloadStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = CreateViewModelWithManager(
            out var manager,
            out _,
            DateDisplayFormats.DayMonthYear);
        manager.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async token =>
            {
                reloadStarted.TrySetResult(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            });

        var reloadTask = sut.ReloadBirdsCommand.ExecuteAsync(null);
        var reloadToken = await reloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        sut.Dispose();

        reloadToken.IsCancellationRequested.Should().BeTrue();
        await reloadTask.WaitAsync(TimeSpan.FromSeconds(3));
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
        return CreateViewModelWithManager(out _, out cache, dateFormat, birds);
    }

    private static BirdListViewModel CreateViewModelWithManager(
        out Mock<IBirdManager> manager,
        out Mock<IBirdViewModelCache> cache,
        string dateFormat,
        params BirdDTO[] birds)
    {
        manager = new Mock<IBirdManager>();
        cache = new Mock<IBirdViewModelCache>();

        return CreateViewModelWithCulture(
            out _,
            () => CultureInfo.GetCultureInfo(AppLanguages.Russian),
            dateFormat,
            manager,
            cache,
            birds);
    }

    private static BirdListViewModel CreateViewModelWithCulture(
        out Mock<ILocalizationService> localization,
        Func<CultureInfo> getCulture,
        string dateFormat,
        params BirdDTO[] birds)
    {
        return CreateViewModelWithCulture(
            out localization,
            getCulture,
            dateFormat,
            null,
            null,
            birds);
    }

    private static BirdListViewModel CreateViewModelWithCulture(
        out Mock<ILocalizationService> localization,
        Func<CultureInfo> getCulture,
        string dateFormat,
        Mock<IBirdManager>? manager = null,
        Mock<IBirdViewModelCache>? cache = null,
        params BirdDTO[] birds)
    {
        var store = new BirdStore();
        store.CompleteLoading();

        foreach (var bird in birds)
            store.Birds.Add(bird);

        manager ??= new Mock<IBirdManager>();
        manager.SetupGet(x => x.Store).Returns(store);

        localization = new Mock<ILocalizationService>();
        localization.SetupGet(x => x.CurrentCulture).Returns(() => getCulture());
        localization.SetupGet(x => x.CurrentDateFormat).Returns(dateFormat);
        localization.Setup(x => x.GetString(It.IsAny<string>()))
            .Returns((string key) => AppText.Get(key, getCulture()));
        localization.Setup(x => x.FormatDate(It.IsAny<DateOnly>(), It.IsAny<DateDisplayStyle>()))
            .Returns((DateOnly value, DateDisplayStyle style) =>
                DateDisplayFormats.FormatDate(value, getCulture(), dateFormat, style));
        localization.Setup(x => x.FormatDate(It.IsAny<DateOnly?>(), It.IsAny<DateDisplayStyle>(), It.IsAny<string?>()))
            .Returns((DateOnly? value, DateDisplayStyle style, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDate(value.Value, getCulture(), dateFormat, style)
                    : fallback ?? "\u2014");

        var birdNameDisplay = new BirdNameDisplayService(localization.Object);
        var birdSearchMatcher = new BirdSearchMatcher(localization.Object, birdNameDisplay);
        cache ??= new Mock<IBirdViewModelCache>();

        return new BirdListViewModel(
            manager.Object,
            localization.Object,
            birdNameDisplay,
            birdSearchMatcher,
            cache.Object,
            new InlineUiDispatcher(),
            TestBackgroundTaskRunner.Create(),
            SearchDebounceDelay);
    }

    private static BirdDTO CreateBird(BirdSpecies species, string? desc = null)
    {
        return TestHelpers.Bird(name: BirdNameDisplayNames.GetDisplayName(species), desc: desc);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(10);
        }

        condition().Should().BeTrue("the expected debounced filter state should eventually be reached");
    }
}
