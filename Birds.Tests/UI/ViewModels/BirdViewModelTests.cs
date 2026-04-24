using System.Globalization;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Common;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using Birds.Shared.Localization;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.ViewModels;

public class BirdViewModelTests
{
    private readonly Mock<IBirdManager> _birdManager = new();
    private readonly IBirdNameDisplayService _birdNameDisplay;
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
            .Returns((DateOnly value, DateDisplayStyle style) =>
                DateDisplayFormats.FormatDate(value, _currentCulture, _currentDateFormat, style));
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

        _birdNameDisplay = new BirdNameDisplayService(_localization.Object);
    }

    [Fact]
    public async Task SaveAsync_Uses_SelectedBirdName_When_Updating()
    {
        var sparrow = (BirdSpecies)1;
        var chickadee = (BirdSpecies)6;

        var original = CreateBirdDto(sparrow);
        var updated = original with
        {
            Species = chickadee,
            Name = BirdNameDisplayNames.GetDisplayName(chickadee, _currentCulture),
            UpdatedAt = DateTime.Now
        };

        BirdUpdateDTO? sentDto = null;
        _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
            .Callback<BirdUpdateDTO, CancellationToken>((dto, _) => sentDto = dto)
            .ReturnsAsync(Result<BirdDTO>.Success(updated));

        var sut = CreateViewModel(original);
        sut.EditCommand.Execute(null);
        sut.SelectedBirdName = chickadee;

        await sut.SaveCommand.ExecuteAsync(null);

        sentDto.Should().NotBeNull();
        sentDto!.Species.Should().Be(chickadee);
        sentDto.Version.Should().Be(original.Version);
        sut.Name.Should().Be(BirdNameDisplayNames.GetDisplayName(chickadee, _currentCulture));
        sut.Dto.Species.Should().Be(chickadee);
        sut.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_Should_Pass_Cancelable_Token_To_Manager()
    {
        var original = CreateBirdDto((BirdSpecies)1);
        CancellationToken capturedToken = default;
        _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
            .Callback<BirdUpdateDTO, CancellationToken>((_, token) => capturedToken = token)
            .ReturnsAsync(Result<BirdDTO>.Success(original));

        var sut = CreateViewModel(original);
        sut.EditCommand.Execute(null);

        await sut.SaveCommand.ExecuteAsync(null);

        capturedToken.CanBeCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_Should_Cancel_Running_Save()
    {
        var original = CreateBirdDto((BirdSpecies)1);
        var saveStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
            .Returns<BirdUpdateDTO, CancellationToken>(async (_, token) =>
            {
                saveStarted.TrySetResult(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return Result<BirdDTO>.Success(original);
            });

        var sut = CreateViewModel(original);
        sut.EditCommand.Execute(null);
        var saveTask = sut.SaveCommand.ExecuteAsync(null);
        var saveToken = await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        sut.Dispose();

        saveToken.IsCancellationRequested.Should().BeTrue();
        await saveTask.WaitAsync(TimeSpan.FromSeconds(3));
        _notification.Verify(
            x => x.ShowErrorLocalized("Error.CannotUpdateBird", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_When_Concurrency_Conflict_Should_Show_Clear_Warning_And_Reload()
    {
        var original = CreateBirdDto((BirdSpecies)1);
        _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BirdDTO>.Failure(ErrorMessages.BirdConcurrencyConflict));
        _birdManager.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateViewModel(original);
        sut.EditCommand.Execute(null);
        sut.Description = "stale edit";

        await sut.SaveCommand.ExecuteAsync(null);

        _notification.Verify(
            x => x.ShowWarning(ErrorMessages.BirdConcurrencyConflict),
            Times.Once);
        _notification.Verify(
            x => x.ShowErrorLocalized("Error.CannotUpdateBird", It.IsAny<object[]>()),
            Times.Never);
        _birdManager.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        sut.Description.Should().Be(original.Description);
        sut.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task CancelEdit_After_Successful_Save_Restores_Latest_Saved_State()
    {
        var sparrow = (BirdSpecies)1;
        var hawfinch = (BirdSpecies)4;
        var chickadee = (BirdSpecies)6;

        var original = CreateBirdDto(sparrow);
        var updated = original with
        {
            Species = chickadee,
            Name = BirdNameDisplayNames.GetDisplayName(chickadee, _currentCulture),
            Description = "updated",
            UpdatedAt = DateTime.Now
        };

        _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BirdDTO>.Success(updated));

        var sut = CreateViewModel(original);
        sut.EditCommand.Execute(null);
        sut.SelectedBirdName = chickadee;
        sut.Description = "updated";
        await sut.SaveCommand.ExecuteAsync(null);

        sut.EditCommand.Execute(null);
        sut.SelectedBirdName = hawfinch;
        sut.Description = "draft";

        sut.CancelEditCommand.Execute(null);

        sut.Name.Should().Be(BirdNameDisplayNames.GetDisplayName(chickadee, _currentCulture));
        sut.SelectedBirdName.Should().Be(chickadee);
        sut.Description.Should().Be("updated");
        sut.Dto.Species.Should().Be(chickadee);
    }

    [Fact]
    public void SaveCommand_Should_Be_Locked_When_Dead_Bird_Has_No_Departure()
    {
        var sparrow = (BirdSpecies)1;
        var dto = CreateBirdDto(sparrow);
        var sut = CreateViewModel(dto);

        sut.EditCommand.Execute(null);
        sut.IsAlive = false;
        sut.Departure = null;

        sut.IsSaveLockedByDepartureRequirement.Should().BeTrue();
        sut.SaveCommand.CanExecute(null).Should().BeFalse();

        sut.Departure = DateOnly.FromDateTime(DateTime.Today);

        sut.IsSaveLockedByDepartureRequirement.Should().BeFalse();
        sut.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DescriptionValidation_Should_Use_Shared_Max_Length()
    {
        var sut = CreateViewModel(CreateBirdDto((BirdSpecies)1));

        sut.Description = new string('A', BirdValidationRules.DescriptionMaxLength);

        sut.GetErrors(nameof(BirdViewModel.Description)).Should().BeEmpty();

        sut.Description = new string('A', BirdValidationRules.DescriptionMaxLength + 1);

        sut.GetErrors(nameof(BirdViewModel.Description)).Should().ContainSingle();
    }

    [Fact]
    public void DepartureValidation_Should_Use_Shared_Date_Range_Rules()
    {
        var dto = CreateBirdDto((BirdSpecies)1) with
        {
            Arrival = BirdValidationRules.MinimumArrivalDate
        };
        var sut = CreateViewModel(dto);

        sut.Departure = BirdValidationRules.MinimumArrivalDate.AddDays(-1);

        GetValidationError(sut, nameof(BirdViewModel.Departure))
            .Should().Be(string.Format(
                ValidationMessages.InvalidDateRange,
                BirdValidationRules.MinimumArrivalDate,
                BirdValidationRules.CurrentLocalDate()));
    }

    [Fact]
    public void DepartureValidation_Should_Accept_Required_Departure_For_DeadBird()
    {
        var sut = CreateViewModel(CreateBirdDto((BirdSpecies)1));

        sut.IsAlive = false;
        sut.Departure = DateOnly.FromDateTime(DateTime.Today);

        sut.GetErrors(nameof(BirdViewModel.Departure)).Should().BeEmpty();
    }

    [Fact]
    public void CultureOrDateFormatChanged_Should_Update_Localized_Display_Fields()
    {
        var chickadee = (BirdSpecies)6;
        var dto = CreateBirdDto(chickadee);
        var sut = CreateViewModel(dto);

        sut.DisplayName.Should().Be(BirdNameDisplayNames.GetDisplayName(chickadee, _currentCulture));
        sut.SelectedBirdName.Should().Be(chickadee);
        sut.DepartureDisplay.Should().Be(AppText.Get("Info.ToThisDay", _currentCulture));
        sut.ArrivalDisplay.Should().Be(DateDisplayFormats.FormatDate(dto.Arrival, _currentCulture, _currentDateFormat));

        _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _currentDateFormat = DateDisplayFormats.YearMonthDay;
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.DisplayName.Should().Be(BirdNameDisplayNames.GetDisplayName(chickadee, _currentCulture));
        sut.SelectedBirdName.Should().Be(chickadee);
        sut.DepartureDisplay.Should().Be(AppText.Get("Info.ToThisDay", _currentCulture));
        sut.ArrivalDisplay.Should().Be(DateDisplayFormats.FormatDate(dto.Arrival, _currentCulture, _currentDateFormat));
    }

    [Fact]
    public void Ctor_Should_NotShiftAlreadyLocalCreatedAtAndUpdatedAt()
    {
        var localCreatedAt = new DateTime(2026, 1, 2, 1, 0, 0, DateTimeKind.Unspecified);
        var localUpdatedAt = new DateTime(2026, 1, 2, 2, 0, 0, DateTimeKind.Unspecified);
        var dto = CreateBirdDto((BirdSpecies)1) with
        {
            CreatedAt = localCreatedAt,
            UpdatedAt = localUpdatedAt
        };

        var sut = CreateViewModel(dto);

        sut.LocalCreatedAt.Should().Be(localCreatedAt);
        sut.LocalUpdatedAt.Should().Be(localUpdatedAt);
    }

    [Fact]
    public void LanguageChanged_Should_Update_DurationDisplay()
    {
        var sparrow = (BirdSpecies)1;
        var dto = CreateBirdDto(sparrow) with
        {
            Arrival = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
            Departure = DateOnly.FromDateTime(DateTime.Today)
        };

        var sut = CreateViewModel(dto);

        sut.DurationDisplay.Should().Be($"3 {AppText.Get("Bird.DaysSuffix", _currentCulture)}");

        _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.DurationDisplay.Should().Be($"3 {AppText.Get("Bird.DaysSuffix", _currentCulture)}");
    }

    [Fact]
    public void Dispose_Should_Unsubscribe_From_Localization_LanguageChanged()
    {
        var chickadee = (BirdSpecies)6;
        var dto = CreateBirdDto(chickadee);
        var sut = CreateViewModel(dto);

        sut.Dispose();
        _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.IsDisposed.Should().BeTrue();
        sut.DisplayName.Should().Be(BirdNameDisplayNames.GetDisplayName(chickadee, CultureInfo.GetCultureInfo(AppLanguages.Russian)));
        _localization.VerifyRemove(x => x.LanguageChanged -= It.IsAny<EventHandler>(), Times.Once);
    }

    [Fact]
    public void LanguageChanged_Should_Rebuild_DepartureValidation_In_Current_Language()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;

            var sparrow = (BirdSpecies)1;
            var dto = CreateBirdDto(sparrow);
            var sut = CreateViewModel(dto);

            sut.EditCommand.Execute(null);
            sut.IsAlive = false;
            sut.Departure = null;

            GetValidationError(sut, nameof(BirdViewModel.Departure))
                .Should().Be(AppText.Get("Validation.DateIsNotSpecified", _currentCulture));

            _currentCulture = CultureInfo.GetCultureInfo(AppLanguages.English);
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;
            _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

            GetValidationError(sut, nameof(BirdViewModel.Departure))
                .Should().Be(AppText.Get("Validation.DateIsNotSpecified", _currentCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUiCulture;
        }
    }

    private static BirdDTO CreateBirdDto(BirdSpecies name)
    {
        return new BirdDTO(
            Guid.NewGuid(),
            name.ToString(),
            "initial",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            null,
            true,
            DateTime.Today.AddDays(-5),
            null)
        {
            Species = name
        };
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

    private static string GetValidationError(BirdViewModel viewModel, string propertyName)
    {
        return viewModel.GetErrors(propertyName)
            .Single()
            .ErrorMessage!;
    }
}
