using System.ComponentModel.DataAnnotations;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Shared.Constants;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

/// <summary>
///     ViewModel representing a single bird, used for display and editing.
///     Inherits common properties and validation rules from <see cref="BirdValidationBaseViewModel" />.
/// </summary>
public partial class BirdViewModel : BirdValidationBaseViewModel
{
    public BirdViewModel(
        BirdDTO dto,
        IBirdManager birdManager,
        ILocalizationService localization,
        INotificationService notificationService)
    {
        _birdManager = birdManager;
        _localization = localization;
        _notificationService = notificationService;

        _localization.LanguageChanged += OnLanguageChanged;
        ErrorsChanged += (_, __) => SaveCommand.NotifyCanExecuteChanged();

        ApplyDto(dto);
        ValidateAllProperties();
    }

    #region [ Fields ]

    private readonly IBirdManager _birdManager;
    private readonly ILocalizationService _localization;
    private readonly INotificationService _notificationService;

    #endregion [ Fields ]

    #region [ Properties ]

    /// <summary>
    ///     The original bird DTO object.
    /// </summary>
    public BirdDTO Dto { get; private set; } = null!;

    /// <summary>
    ///     The unique identifier of the bird.
    /// </summary>
    public Guid Id => Dto.Id;

    /// <summary>
    ///     Bird's creation date in local time.
    /// </summary>
    public DateTime? LocalCreatedAt { get; private set; }

    /// <summary>
    ///     Indicates that saving is intentionally blocked because a dead bird requires a departure date.
    /// </summary>
    public bool IsSaveLockedByDepartureRequirement => !IsAlive && Departure is null;

    #endregion [ Properties ]

    #region [ ObservableProperties ]

    /// <summary>
    ///     The bird’s name.
    /// </summary>
    [ObservableProperty] private string name = string.Empty;

    /// <summary>
    ///     The localized display name of the bird.
    /// </summary>
    [ObservableProperty] private string displayName = string.Empty;

    /// <summary>
    ///     A culture-aware arrival date string.
    /// </summary>
    [ObservableProperty] private string arrivalDisplay = string.Empty;

    /// <summary>
    ///     The departure date of the bird (if it has left the record).
    /// </summary>
    [CustomValidation(typeof(BirdViewModel), nameof(ValidateDeparture))]
    [ObservableProperty]
    private DateOnly? departure;

    /// <summary>
    ///     Indicates whether the bird is alive.
    /// </summary>
    [ObservableProperty] private bool isAlive;

    /// <summary>
    ///     The number of days since arrival (or until departure).
    /// </summary>
    [ObservableProperty] private int daysInStock;

    /// <summary>
    ///     Localized duration text for the current culture.
    /// </summary>
    [ObservableProperty] private string durationDisplay = string.Empty;

    /// <summary>
    ///     Localized bird status text for the current culture.
    /// </summary>
    [ObservableProperty] private string statusText = string.Empty;

    /// <summary>
    ///     A textual representation of the departure date (or “to this day” if none).
    /// </summary>
    [ObservableProperty] private string departureDisplay = string.Empty;

    /// <summary>
    ///     Determines whether delete confirmation buttons are visible.
    /// </summary>
    [ObservableProperty] private bool isConfirmingDelete;

    /// <summary>
    ///     Indicates whether the item is in edit mode.
    /// </summary>
    [ObservableProperty] private bool isEditing;

    /// <summary>
    ///     Bird's last updated date in local time.
    /// </summary>
    [ObservableProperty] private DateTime? localUpdatedAt;

    /// <summary>
    ///     Bird's creation date formatted for the current UI culture.
    /// </summary>
    [ObservableProperty] private string localCreatedAtDisplay = string.Empty;

    /// <summary>
    ///     Bird's last updated date formatted for the current UI culture.
    /// </summary>
    [ObservableProperty] private string localUpdatedAtDisplay = string.Empty;

    #endregion [ ObservableProperties ]

    #region [ Commands ]

    /// <summary>
    ///     Command for deleting a bird.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        var result = await _birdManager.DeleteAsync(Id, CancellationToken.None);

        if (!result.IsSuccess)
            _notificationService.ShowErrorLocalized("Error.CannotDeleteBird");

        IsConfirmingDelete = false;
    }

    /// <summary>
    ///     Command for displaying delete confirmation buttons.
    /// </summary>
    [RelayCommand]
    private void AskDelete()
    {
        IsConfirmingDelete = true;
    }

    /// <summary>
    ///     Command for canceling delete confirmation.
    /// </summary>
    [RelayCommand]
    private void CancelDelete()
    {
        IsConfirmingDelete = false;
    }

    /// <summary>
    ///     Command for switching to edit mode.
    /// </summary>
    [RelayCommand]
    private void Edit()
    {
        IsEditing = true;
    }

    /// <summary>
    ///     Command for canceling editing and reverting changes.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        ApplyDto(Dto);
        IsEditing = false;
    }

    /// <summary>
    ///     Command that validates user input and updates the bird through <see cref="IBirdManager" />.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors)
            return;

        if (SelectedBirdName is not BirdsName selectedBirdName)
        {
            ValidateProperty(SelectedBirdName, nameof(SelectedBirdName));
            return;
        }

        _notificationService.ShowInfoLocalized("Info.UpdatingBird");

        var result = await _birdManager.UpdateAsync(
            new BirdUpdateDTO(
                Id,
                selectedBirdName.ToString(),
                Description,
                Arrival,
                Departure,
                IsAlive), CancellationToken.None);

        if (result.IsSuccess)
        {
            ApplyDto(result.Value);
            _notificationService.ShowSuccessLocalized("Info.UpdatedBird");
        }
        else
        {
            CancelEdit();
            _notificationService.ShowErrorLocalized("Error.CannotUpdateBird");
        }

        IsEditing = false;
    }

    /// <summary>
    ///     For the DatePicker to be able to clear the departure date.
    /// </summary>
    [RelayCommand]
    private void ClearDeparture()
    {
        Departure = null;
    }

    private bool CanSave()
    {
        return !HasErrors;
    }

    #endregion [ Commands ]

    #region [ Private helpers ]

    /// <summary>
    ///     Updates calculated fields and all culture-aware display values.
    /// </summary>
    private void UpdateCalculatedFields()
    {
        var culture = _localization.CurrentCulture;

        DisplayName = ResolveDisplayName();
        ArrivalDisplay = _localization.FormatDate(Arrival);
        DepartureDisplay = Departure.HasValue
            ? _localization.FormatDate(Departure.Value)
            : _localization.GetString("Info.ToThisDay");

        var endDate = Departure?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
        DaysInStock = (int)(endDate - Arrival.ToDateTime(TimeOnly.MinValue)).TotalDays;
        DurationDisplay = $"{DaysInStock} {_localization.GetString("Bird.DaysSuffix")}";
        StatusText = _localization.GetString(IsAlive ? "Bird.StatusAlive" : "Bird.StatusDead");
        LocalCreatedAtDisplay = _localization.FormatDateTime(LocalCreatedAt);
        LocalUpdatedAtDisplay = _localization.FormatDateTime(LocalUpdatedAt);
    }

    /// <summary>
    ///     Synchronizes the editable state with a DTO returned from the application layer.
    /// </summary>
    private void ApplyDto(BirdDTO dto)
    {
        Dto = dto ?? throw new ArgumentNullException(nameof(dto));

        Name = dto.Name;
        SelectedBirdName = BirdEnumHelper.ParseBirdName(dto.Name);
        Description = dto.Description;
        Arrival = dto.Arrival;
        Departure = dto.Departure;
        IsAlive = dto.IsAlive;
        LocalCreatedAt = dto.CreatedAt;
        LocalUpdatedAt = dto.UpdatedAt;

        UpdateCalculatedFields();
    }

    private string ResolveDisplayName()
    {
        if (SelectedBirdName is { } selectedBirdName)
            return selectedBirdName.ToDisplayName(_localization.CurrentCulture);

        return BirdEnumHelper.ParseBirdName(Name)?.ToDisplayName(_localization.CurrentCulture)
               ?? Name;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateCalculatedFields();
        ValidateProperty(SelectedBirdName, nameof(SelectedBirdName));
        ValidateProperty(Description, nameof(Description));
        ValidateProperty(Arrival, nameof(Arrival));
        ValidateProperty(Departure, nameof(Departure));
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    ///     Automatically invoked when the departure date changes (partial method generated by the Toolkit).
    /// </summary>
    partial void OnDepartureChanged(DateOnly? value)
    {
        ValidateProperty(value, nameof(Departure));
        UpdateCalculatedFields();
        OnPropertyChanged(nameof(IsSaveLockedByDepartureRequirement));
    }

    /// <summary>
    ///     Overrides logic executed when the arrival date changes.
    /// </summary>
    protected override void OnArrivalChangedCore(DateOnly value)
    {
        ValidateProperty(Departure, nameof(Departure));
        UpdateCalculatedFields();
    }

    /// <summary>
    ///     Keeps the display name aligned with the selected enum value while editing.
    /// </summary>
    protected override void OnSelectedBirdNameChangedCore(BirdsName? value)
    {
        if (value is { } selectedBirdName)
            Name = selectedBirdName.ToString();

        UpdateCalculatedFields();
    }

    #endregion [ Private helpers ]

    #region [ Validation ]

    /// <summary>
    ///     Validates the departure date.
    ///     Allows null, disallows future dates and dates earlier than Arrival.
    /// </summary>
    public static ValidationResult? ValidateDeparture(object? value, ValidationContext ctx)
    {
        if (ctx.ObjectInstance is BirdViewModel birdVM && value is null && !birdVM.IsAlive)
            return new ValidationResult(ValidationMessages.DateIsNotSpecified);

        if (value is null)
            return ValidationResult.Success;

        if (value is not DateOnly d)
            return new ValidationResult(ValidationMessages.DateIsNotValid);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (d > today)
            return new ValidationResult(
                string.Format(ValidationMessages.DateCannotBeInTheFuture, today));

        if (ctx.ObjectInstance is BirdValidationBaseViewModel vm && d < vm.Arrival)
            return new ValidationResult(
                string.Format(ValidationMessages.DepartureLaterThenArrival, vm.Arrival));

        return ValidationResult.Success;
    }

    partial void OnIsAliveChanged(bool value)
    {
        ValidateProperty(Departure, nameof(Departure));
        UpdateCalculatedFields();
        OnPropertyChanged(nameof(IsSaveLockedByDepartureRequirement));
    }

    #endregion [ Validation ]
}
