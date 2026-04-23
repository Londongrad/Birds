using System.Globalization;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Caching;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.Services;

public class BirdViewModelCacheTests
{
    private readonly IBirdNameDisplayService _birdNameDisplay;
    private readonly Mock<IBirdManager> _birdManager = new();
    private readonly Mock<ILocalizationService> _localization = new();
    private readonly Mock<INotificationService> _notification = new();
    private CultureInfo _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
    private readonly string _dateFormat = DateDisplayFormats.DayMonthYear;

    public BirdViewModelCacheTests()
    {
        _localization.SetupGet(x => x.CurrentCulture).Returns(() => _currentCulture);
        _localization.SetupGet(x => x.CurrentDateFormat).Returns(_dateFormat);
        _localization
            .Setup(x => x.GetString(It.IsAny<string>()))
            .Returns((string key) => AppText.Get(key, _currentCulture));
        _localization
            .Setup(x => x.FormatDate(It.IsAny<DateOnly>(), It.IsAny<DateDisplayStyle>()))
            .Returns((DateOnly value, DateDisplayStyle style) =>
                DateDisplayFormats.FormatDate(value, _currentCulture, _dateFormat, style));
        _localization
            .Setup(x => x.FormatDate(It.IsAny<DateOnly?>(), It.IsAny<DateDisplayStyle>(), It.IsAny<string?>()))
            .Returns((DateOnly? value, DateDisplayStyle style, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDate(value.Value, _currentCulture, _dateFormat, style)
                    : fallback ?? "\u2014");
        _localization
            .Setup(x => x.FormatDateTime(It.IsAny<DateTime>()))
            .Returns((DateTime value) => DateDisplayFormats.FormatDateTime(value, _currentCulture, _dateFormat));
        _localization
            .Setup(x => x.FormatDateTime(It.IsAny<DateTime?>(), It.IsAny<string?>()))
            .Returns((DateTime? value, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDateTime(value.Value, _currentCulture, _dateFormat)
                    : fallback ?? "\u2014");

        _birdNameDisplay = new BirdNameDisplayService(_localization.Object);
    }

    [Fact]
    public void GetOrCreate_Should_Return_Same_ViewModel_For_Same_Bird_While_Cached()
    {
        var dto = CreateBirdDto((BirdSpecies)1);
        var cache = CreateCache(2, out _, out var factory);

        var first = cache.GetOrCreate(dto);
        var second = cache.GetOrCreate(dto);

        second.Should().BeSameAs(first);
        factory.Verify(x => x.Create(dto), Times.Once);
    }

    [Fact]
    public void GetOrCreate_Should_Evict_Least_Recently_Used_ViewModel_When_MaxSizeExceeded()
    {
        var firstDto = CreateBirdDto((BirdSpecies)1);
        var secondDto = CreateBirdDto((BirdSpecies)2);
        var thirdDto = CreateBirdDto((BirdSpecies)3);
        var cache = CreateCache(2, out _, out _);

        var first = cache.GetOrCreate(firstDto);
        var second = cache.GetOrCreate(secondDto);
        _ = cache.GetOrCreate(firstDto);
        var third = cache.GetOrCreate(thirdDto);

        cache.Count.Should().Be(2);
        first.IsDisposed.Should().BeFalse();
        second.IsDisposed.Should().BeTrue();
        third.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Clear_Should_Dispose_All_Cached_ViewModels()
    {
        var cache = CreateCache(3, out _, out _);
        var first = cache.GetOrCreate(CreateBirdDto((BirdSpecies)1));
        var second = cache.GetOrCreate(CreateBirdDto((BirdSpecies)2));

        cache.Clear();

        cache.Count.Should().Be(0);
        first.IsDisposed.Should().BeTrue();
        second.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Remove_Should_Dispose_Only_Removed_ViewModel()
    {
        var firstDto = CreateBirdDto((BirdSpecies)1);
        var secondDto = CreateBirdDto((BirdSpecies)2);
        var cache = CreateCache(2, out _, out _);
        var first = cache.GetOrCreate(firstDto);
        var second = cache.GetOrCreate(secondDto);

        cache.Remove(firstDto.Id);

        cache.Count.Should().Be(1);
        first.IsDisposed.Should().BeTrue();
        second.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void GetOrCreate_Should_Not_Evict_Active_Editing_ViewModels()
    {
        var firstDto = CreateBirdDto((BirdSpecies)1);
        var secondDto = CreateBirdDto((BirdSpecies)2);
        var cache = CreateCache(1, out _, out _);
        var first = cache.GetOrCreate(firstDto);
        first.IsEditing = true;

        var second = cache.GetOrCreate(secondDto);

        cache.Count.Should().Be(2);
        first.IsDisposed.Should().BeFalse();
        second.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Refresh_Should_Not_Overwrite_Active_Edit()
    {
        var dto = CreateBirdDto((BirdSpecies)1) with { Description = "saved" };
        var updated = dto with { Description = "server update" };
        var cache = CreateCache(2, out _, out _);
        var vm = cache.GetOrCreate(dto);
        vm.IsEditing = true;
        vm.Description = "draft";

        cache.Refresh(updated);

        vm.Description.Should().Be("draft");
        vm.Dto.Should().Be(dto);
    }

    [Fact]
    public void Refresh_Should_Update_Cached_ViewModel_When_Not_Editing()
    {
        var dto = CreateBirdDto((BirdSpecies)1) with { Description = "saved" };
        var updated = dto with { Description = "server update" };
        var cache = CreateCache(2, out _, out _);
        var vm = cache.GetOrCreate(dto);

        cache.Refresh(updated);

        vm.Description.Should().Be("server update");
        vm.Dto.Should().Be(updated);
    }

    [Fact]
    public void Cached_ViewModel_Should_Update_DisplayName_When_Language_Changes()
    {
        var species = (BirdSpecies)6;
        var dto = CreateBirdDto(species);
        var cache = CreateCache(2, out _, out _);
        var vm = cache.GetOrCreate(dto);

        _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        vm.DisplayName.Should().Be(BirdNameDisplayNames.GetDisplayName(species, _currentCulture));
    }

    private BirdViewModelCache CreateCache(
        int maxSize,
        out List<BirdViewModel> created,
        out Mock<IBirdViewModelFactory> factory)
    {
        created = [];
        var createdViewModels = created;
        factory = new Mock<IBirdViewModelFactory>();
        factory
            .Setup(x => x.Create(It.IsAny<BirdDTO>()))
            .Returns((BirdDTO dto) =>
            {
                var viewModel = CreateViewModel(dto);
                createdViewModels.Add(viewModel);
                return viewModel;
            });

        return new BirdViewModelCache(factory.Object, maxSize);
    }

    private BirdViewModel CreateViewModel(BirdDTO dto)
    {
        return new BirdViewModel(
            dto,
            _birdManager.Object,
            _localization.Object,
            _birdNameDisplay,
            _notification.Object);
    }

    private static BirdDTO CreateBirdDto(BirdSpecies species)
    {
        return new BirdDTO(
            Guid.NewGuid(),
            species.ToString(),
            "description",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            null,
            true,
            DateTime.Today.AddDays(-1),
            null)
        {
            Species = species
        };
    }
}
