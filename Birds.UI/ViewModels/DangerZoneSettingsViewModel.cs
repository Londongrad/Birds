using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

public partial class DangerZoneSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IAutoExportCoordinator _autoExportCoordinator;
    private readonly IBirdManager _birdManager;
    private readonly IDatabaseMaintenanceService _databaseMaintenanceService;
    private readonly INotificationService _notificationService;
    private readonly IAppPreferencesService _preferences;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _disposed;
    private CancellationTokenSource? _dangerActionCancellation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BeginClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginResetLocalDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
    private bool isExternalBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDangerConfirmationVisible))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
    private bool isConfirmingClearBirdRecords;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDangerConfirmationVisible))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
    private bool isConfirmingResetLocalDatabase;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BeginClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginResetLocalDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
    private bool isDangerZoneBusy;

    public DangerZoneSettingsViewModel(
        IAppPreferencesService preferences,
        IBirdManager birdManager,
        IAutoExportCoordinator autoExportCoordinator,
        INotificationService notificationService,
        IDatabaseMaintenanceService databaseMaintenanceService)
    {
        _preferences = preferences;
        _birdManager = birdManager;
        _autoExportCoordinator = autoExportCoordinator;
        _notificationService = notificationService;
        _databaseMaintenanceService = databaseMaintenanceService;
    }

    public event EventHandler? DangerConfirmationStarted;

    public bool SupportsLocalDatabaseReset => _databaseMaintenanceService.CanResetLocalDatabase;

    public bool IsDangerConfirmationVisible => IsConfirmingClearBirdRecords || IsConfirmingResetLocalDatabase;

    public void SetExternalBusy(bool value)
    {
        IsExternalBusy = value;
    }

    public void CancelPendingConfirmations()
    {
        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _dangerActionCancellation?.Cancel();
        DangerConfirmationStarted = null;
        _lifetimeCancellation.Dispose();
    }

    [RelayCommand]
    private void ResetPreferences()
    {
        _preferences.ResetToDefaults();
    }

    [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
    private void BeginClearBirdRecords()
    {
        IsConfirmingResetLocalDatabase = false;
        IsConfirmingClearBirdRecords = true;
        DangerConfirmationStarted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
    private void BeginResetLocalDatabase()
    {
        if (!SupportsLocalDatabaseReset)
            return;

        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = true;
        DangerConfirmationStarted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CancelDangerAction()
    {
        CancelPendingConfirmations();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmClearBirdRecords))]
    private async Task ConfirmClearBirdRecordsAsync(CancellationToken cancellationToken)
    {
        if (!CanConfirmClearBirdRecords())
            return;

        var operationCancellation = CreateDangerActionCancellation(cancellationToken);
        await ExecuteDangerActionAsync(async token =>
        {
            await _birdManager.FlushPendingOperationsAsync(token);
            var removed = await _databaseMaintenanceService.ClearBirdRecordsAsync(token);
            if (_disposed)
                return;

            _birdManager.Store.ReplaceBirds(Array.Empty<BirdDTO>());
            _birdManager.Store.CompleteLoading();
            _autoExportCoordinator.MarkDirty();
            IsConfirmingClearBirdRecords = false;
            _notificationService.ShowSuccessLocalized("Info.BirdRecordsCleared", removed);
        }, "Error.CannotClearBirdRecords", operationCancellation);
    }

    [RelayCommand(CanExecute = nameof(CanConfirmResetLocalDatabase))]
    private async Task ConfirmResetLocalDatabaseAsync(CancellationToken cancellationToken)
    {
        if (!CanConfirmResetLocalDatabase())
            return;

        var operationCancellation = CreateDangerActionCancellation(cancellationToken);
        await ExecuteDangerActionAsync(async token =>
        {
            await _birdManager.FlushPendingOperationsAsync(token);
            await _databaseMaintenanceService.ResetLocalDatabaseAsync(token);
            if (_disposed)
                return;

            _birdManager.Store.ReplaceBirds(Array.Empty<BirdDTO>());
            _birdManager.Store.CompleteLoading();
            _autoExportCoordinator.MarkDirty();
            IsConfirmingResetLocalDatabase = false;
            _notificationService.ShowSuccessLocalized("Info.LocalDatabaseReset");
        }, "Error.CannotResetLocalDatabase", operationCancellation);
    }

    private async Task ExecuteDangerActionAsync(
        Func<CancellationToken, Task> action,
        string errorKey,
        CancellationTokenSource operationCancellation)
    {
        IsDangerZoneBusy = true;
        try
        {
            await action(operationCancellation.Token);
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized(errorKey);
        }
        finally
        {
            if (!_disposed)
                IsDangerZoneBusy = false;

            ClearDangerActionCancellation(operationCancellation);
        }
    }

    private bool CanStartDangerAction()
    {
        return !IsExternalBusy && !IsDangerZoneBusy;
    }

    private bool CanConfirmClearBirdRecords()
    {
        return IsConfirmingClearBirdRecords && CanStartDangerAction();
    }

    private bool CanConfirmResetLocalDatabase()
    {
        return SupportsLocalDatabaseReset && IsConfirmingResetLocalDatabase && CanStartDangerAction();
    }

    private CancellationTokenSource CreateDangerActionCancellation(CancellationToken cancellationToken)
    {
        var previous = _dangerActionCancellation;
        var current = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        _dangerActionCancellation = current;

        previous?.Cancel();
        previous?.Dispose();

        return current;
    }

    private void ClearDangerActionCancellation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_dangerActionCancellation, operationCancellation))
            _dangerActionCancellation = null;

        operationCancellation.Dispose();
    }
}
