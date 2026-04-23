using System.Globalization;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.Services;

public class BirdViewModelFactoryTests
{
    private readonly Mock<ILocalizationService> _localization = new();
    private readonly Mock<IBirdManager> _manager = new();
    private readonly Mock<INotificationService> _notify = new();
    private readonly Mock<IBirdNameDisplayService> _birdNameDisplay = new();

    public BirdViewModelFactoryTests()
    {
        var culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        var dateFormat = DateDisplayFormats.DayMonthYear;

        _localization.SetupGet(x => x.CurrentCulture).Returns(culture);
        _localization.SetupGet(x => x.CurrentDateFormat).Returns(dateFormat);
        _localization
            .Setup(x => x.GetString(It.IsAny<string>()))
            .Returns((string key) => AppText.Get(key, culture));
        _localization
            .Setup(x => x.FormatDate(It.IsAny<DateOnly>(), It.IsAny<DateDisplayStyle>()))
            .Returns((DateOnly value, DateDisplayStyle style) =>
                DateDisplayFormats.FormatDate(value, culture, dateFormat, style));
        _localization
            .Setup(x => x.FormatDate(It.IsAny<DateOnly?>(), It.IsAny<DateDisplayStyle>(), It.IsAny<string?>()))
            .Returns((DateOnly? value, DateDisplayStyle style, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDate(value.Value, culture, dateFormat, style)
                    : fallback ?? "\u2014");
        _localization
            .Setup(x => x.FormatDateTime(It.IsAny<DateTime>()))
            .Returns((DateTime value) => DateDisplayFormats.FormatDateTime(value, culture, dateFormat));
        _localization
            .Setup(x => x.FormatDateTime(It.IsAny<DateTime?>(), It.IsAny<string?>()))
            .Returns((DateTime? value, string? fallback) =>
                value.HasValue
                    ? DateDisplayFormats.FormatDateTime(value.Value, culture, dateFormat)
                    : fallback ?? "\u2014");
        _birdNameDisplay
            .Setup(x => x.GetDisplayName(It.IsAny<BirdsName>()))
            .Returns((BirdsName value) => value.ToString());
    }

    [Fact]
    public void Ctor_Throws_When_Dependencies_Null()
    {
        Action a1 = () => new BirdViewModelFactory(null!, _localization.Object, _birdNameDisplay.Object,
            _notify.Object);
        Action a2 = () => new BirdViewModelFactory(_manager.Object, null!, _birdNameDisplay.Object, _notify.Object);
        Action a3 = () => new BirdViewModelFactory(_manager.Object, _localization.Object, null!, _notify.Object);
        Action a4 = () => new BirdViewModelFactory(_manager.Object, _localization.Object, _birdNameDisplay.Object,
            null!);

        a1.Should().Throw<ArgumentNullException>().WithParameterName("birdManager");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("localization");
        a3.Should().Throw<ArgumentNullException>().WithParameterName("birdNameDisplay");
        a4.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    [Fact]
    public void Create_Returns_ViewModel_And_Does_Not_Call_Services()
    {
        var factory = new BirdViewModelFactory(_manager.Object, _localization.Object, _birdNameDisplay.Object,
            _notify.Object);

        var dto = new BirdDTO(
            Guid.NewGuid(),
            "Sparrow",
            "desc",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-2)),
            null,
            true,
            DateTime.Now.AddDays(-2),
            null);

        var vm = factory.Create(dto);

        vm.Should().NotBeNull().And.BeOfType<BirdViewModel>();
        vm.Dto.Should().Be(dto);

        _manager.VerifyNoOtherCalls();
        _notify.VerifyNoOtherCalls();
    }

    [Fact]
    public void Create_Throws_When_Dto_Null()
    {
        var factory = new BirdViewModelFactory(_manager.Object, _localization.Object, _birdNameDisplay.Object,
            _notify.Object);
        Action act = () => factory.Create(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dto");
    }
}
