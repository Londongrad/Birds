using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;
using System.Globalization;

namespace Birds.Tests.UI.ViewModels
{
    public class BirdViewModelTests
    {
        private readonly Mock<IBirdManager> _birdManager = new();
        private readonly Mock<ILocalizationService> _localization = new();
        private readonly Mock<INotificationService> _notification = new();
        private CultureInfo _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        private string _currentDateFormat = DateDisplayFormats.DayMonthYear;

        public BirdViewModelTests()
        {
            _localization.SetupGet(x => x.CurrentCulture).Returns(() => _currentCulture);
            _localization.SetupGet(x => x.CurrentDateFormat).Returns(() => _currentDateFormat);
            _localization
                .Setup(x => x.GetString(It.IsAny<string>()))
                .Returns((string key) => AppText.Get(key, _currentCulture));
            _localization
                .Setup(x => x.FormatDate(It.IsAny<DateOnly>(), It.IsAny<DateDisplayStyle>()))
                .Returns((DateOnly value, DateDisplayStyle style) => DateDisplayFormats.FormatDate(value, _currentCulture, _currentDateFormat, style));
            _localization
                .Setup(x => x.FormatDate(It.IsAny<DateOnly?>(), It.IsAny<DateDisplayStyle>(), It.IsAny<string?>()))
                .Returns((DateOnly? value, DateDisplayStyle style, string? fallback) =>
                    value.HasValue
                        ? DateDisplayFormats.FormatDate(value.Value, _currentCulture, _currentDateFormat, style)
                        : fallback ?? "\u2014");
            _localization
                .Setup(x => x.FormatDateTime(It.IsAny<DateTime>()))
                .Returns((DateTime value) => DateDisplayFormats.FormatDateTime(value, _currentCulture, _currentDateFormat));
            _localization
                .Setup(x => x.FormatDateTime(It.IsAny<DateTime?>(), It.IsAny<string?>()))
                .Returns((DateTime? value, string? fallback) =>
                    value.HasValue
                        ? DateDisplayFormats.FormatDateTime(value.Value, _currentCulture, _currentDateFormat)
                        : fallback ?? "\u2014");
        }

        [Fact]
        public async Task SaveAsync_Uses_SelectedBirdName_When_Updating()
        {
            var sparrow = (BirdsName)1;
            var chickadee = (BirdsName)6;

            var original = CreateBirdDto(sparrow);
            var updated = original with
            {
                Name = chickadee.ToString(),
                UpdatedAt = DateTime.Now
            };

            BirdUpdateDTO? sentDto = null;
            _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
                .Callback<BirdUpdateDTO, CancellationToken>((dto, _) => sentDto = dto)
                .ReturnsAsync(Result<BirdDTO>.Success(updated));

            var sut = new BirdViewModel(original, _birdManager.Object, _localization.Object, _notification.Object);
            sut.EditCommand.Execute(null);
            sut.SelectedBirdName = chickadee;

            await sut.SaveCommand.ExecuteAsync(null);

            sentDto.Should().NotBeNull();
            sentDto!.Name.Should().Be(chickadee.ToString());
            sut.Name.Should().Be(chickadee.ToString());
            sut.Dto.Name.Should().Be(chickadee.ToString());
            sut.IsEditing.Should().BeFalse();
        }

        [Fact]
        public async Task CancelEdit_After_Successful_Save_Restores_Latest_Saved_State()
        {
            var sparrow = (BirdsName)1;
            var hawfinch = (BirdsName)4;
            var chickadee = (BirdsName)6;

            var original = CreateBirdDto(sparrow);
            var updated = original with
            {
                Name = chickadee.ToString(),
                Description = "updated",
                UpdatedAt = DateTime.Now
            };

            _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<BirdDTO>.Success(updated));

            var sut = new BirdViewModel(original, _birdManager.Object, _localization.Object, _notification.Object);
            sut.EditCommand.Execute(null);
            sut.SelectedBirdName = chickadee;
            sut.Description = "updated";
            await sut.SaveCommand.ExecuteAsync(null);

            sut.EditCommand.Execute(null);
            sut.SelectedBirdName = hawfinch;
            sut.Description = "draft";

            sut.CancelEditCommand.Execute(null);

            sut.Name.Should().Be(chickadee.ToString());
            sut.SelectedBirdName.Should().Be(chickadee);
            sut.Description.Should().Be("updated");
            sut.Dto.Name.Should().Be(chickadee.ToString());
        }

        [Fact]
        public void CultureOrDateFormatChanged_Should_Update_Localized_Display_Fields()
        {
            var chickadee = (BirdsName)6;
            var dto = CreateBirdDto(chickadee);
            var sut = new BirdViewModel(dto, _birdManager.Object, _localization.Object, _notification.Object);

            sut.DisplayName.Should().Be(chickadee.ToDisplayName(_currentCulture));
            sut.DepartureDisplay.Should().Be(AppText.Get("Info.ToThisDay", _currentCulture));
            sut.ArrivalDisplay.Should().Be(DateDisplayFormats.FormatDate(dto.Arrival, _currentCulture, _currentDateFormat));

            _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
            _currentDateFormat = DateDisplayFormats.YearMonthDay;
            _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

            sut.DisplayName.Should().Be(chickadee.ToDisplayName(_currentCulture));
            sut.DepartureDisplay.Should().Be(AppText.Get("Info.ToThisDay", _currentCulture));
            sut.ArrivalDisplay.Should().Be(DateDisplayFormats.FormatDate(dto.Arrival, _currentCulture, _currentDateFormat));
        }

        [Fact]
        public void LanguageChanged_Should_Update_DurationDisplay()
        {
            var sparrow = (BirdsName)1;
            var dto = CreateBirdDto(sparrow) with
            {
                Arrival = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                Departure = DateOnly.FromDateTime(DateTime.Today)
            };

            var sut = new BirdViewModel(dto, _birdManager.Object, _localization.Object, _notification.Object);

            sut.DurationDisplay.Should().Be($"3 {AppText.Get("Bird.DaysSuffix", _currentCulture)}");

            _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
            _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

            sut.DurationDisplay.Should().Be($"3 {AppText.Get("Bird.DaysSuffix", _currentCulture)}");
        }

        private static BirdDTO CreateBirdDto(BirdsName name) =>
            new(
                Id: Guid.NewGuid(),
                Name: name.ToString(),
                Description: "initial",
                Arrival: DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                Departure: null,
                IsAlive: true,
                CreatedAt: DateTime.Today.AddDays(-5),
                UpdatedAt: null);
    }
}
