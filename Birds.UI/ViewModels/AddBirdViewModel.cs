using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using System.ComponentModel;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

/// <summary>
///     ViewModel for the "Add Bird" view.
///     Inherits common fields and validation rules from <see cref="BirdValidationBaseViewModel" />.
/// </summary>
/// <remarks>
///     Contains commands for saving a new bird and notifying the user about the result.
/// </remarks>
public partial class AddBirdViewModel : BirdValidationBaseViewModel, IDisposable
{
    /// <summary>
    ///     Creates a new instance of <see cref="AddBirdViewModel" />.
    /// </summary>
    /// <param name="mediator">The MediatR mediator used to send application commands.</param>
    /// <param name="notification">The user notification service.</param>
    public AddBirdViewModel(INotificationService notification, IBirdManager birdManager)
    {
        _notification = notification;
        _birdManager = birdManager;

        ErrorsChanged += OnErrorsChanged;

        // Run initial validation so that the Save button is disabled by default
        ValidateAllProperties();
    }

    #region [ Methods ]

    /// <summary>
    ///     Determines whether the Save command can be executed.
    /// </summary>
    private bool CanSave()
    {
        return !HasErrors && !IsBusy;
    }

    #endregion [ Methods ]

    #region [ Commands ]

    /// <summary>
    ///     Command to save a new bird to the system.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            return;

        // Force validation of all properties before saving
        ValidateAllProperties();
        if (HasErrors)
            return;

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        var operationToken = operationCancellation.Token;
        IsBusy = true; // Disable button
        SaveCommand.NotifyCanExecuteChanged();

        _notification.ShowInfoLocalized("Info.AddingBird");

        var dto = new BirdCreateDTO(
            SelectedBirdName ?? default,
            Description,
            Arrival,
            IsOneTime ? Arrival : null,
            !IsOneTime
        );

        try
        {
            var result = await _birdManager.AddAsync(dto, operationToken);
            if (_disposed)
                return;

            if (result.IsSuccess)
            {
                _notification.ShowSuccessLocalized("Info.BirdAdded");
                // Reset the description after successful save
                Description = string.Empty;
            }
            else
            {
                _notification.ShowErrorLocalized("Error.CannotSaveBird");
            }
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
        {
            // The save was canceled because the command, view model, or app lifetime ended.
        }
        finally
        {
            if (!_disposed)
            {
                IsBusy = false; // Enable button again
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    #endregion [ Commands ]

    #region [ Fields ]

    private readonly INotificationService _notification;
    private readonly IBirdManager _birdManager;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _disposed;

    #endregion [ Fields ]

    #region [ Observable Properties ]

    [ObservableProperty] private bool isBusy; // Indicates whether the add operation is in progress

    [ObservableProperty] private bool isOneTime;

    #endregion [ Observable Properties ]

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
        ErrorsChanged -= OnErrorsChanged;
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        if (!_disposed)
            SaveCommand.NotifyCanExecuteChanged();
    }
}
