using System.ComponentModel;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly AppearanceSettingsViewModel _appearanceSettings;
    private readonly IAutoExportCoordinator _autoExportCoordinator;
    private readonly IBirdManager _birdManager;
    private readonly IDatabaseMaintenanceService _databaseMaintenanceService;
    private readonly ImportExportSettingsViewModel _importExportSettings;
    private readonly INotificationService _notificationService;
    private readonly IAppPreferencesService _preferences;
    private readonly SyncSettingsViewModel _syncSettings;
    private bool _disposed;

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

    public SettingsViewModel(IAppPreferencesService preferences,
        IBirdManager birdManager,
        IAutoExportCoordinator autoExportCoordinator,
        INotificationService notificationService,
        IDatabaseMaintenanceService databaseMaintenanceService,
        AppearanceSettingsViewModel appearanceSettings,
        ImportExportSettingsViewModel importExportSettings,
        SyncSettingsViewModel syncSettings)
    {
        _preferences = preferences;
        _birdManager = birdManager;
        _autoExportCoordinator = autoExportCoordinator;
        _notificationService = notificationService;
        _databaseMaintenanceService = databaseMaintenanceService;
        _appearanceSettings = appearanceSettings;
        _importExportSettings = importExportSettings;
        _syncSettings = syncSettings;

        _importExportSettings.PropertyChanged += OnImportExportSettingsPropertyChanged;
        _syncSettings.SyncConfirmationStarted += OnSyncConfirmationStarted;
        UpdateChildExternalBusy();
    }

    public AppearanceSettingsViewModel AppearanceSettings => _appearanceSettings;

    public ImportExportSettingsViewModel ImportExportSettings => _importExportSettings;

    public SyncSettingsViewModel SyncSettings => _syncSettings;

    public bool SupportsLocalDatabaseReset => _databaseMaintenanceService.CanResetLocalDatabase;

    public bool IsDangerConfirmationVisible => IsConfirmingClearBirdRecords || IsConfirmingResetLocalDatabase;

    [RelayCommand]
    private void ResetPreferences()
    {
        _preferences.ResetToDefaults();
    }

    [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
    private void BeginClearBirdRecords()
    {
        SyncSettings.CancelPendingConfirmations();
        IsConfirmingResetLocalDatabase = false;
        IsConfirmingClearBirdRecords = true;
    }

    [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
    private void BeginResetLocalDatabase()
    {
        if (!SupportsLocalDatabaseReset)
            return;

        SyncSettings.CancelPendingConfirmations();
        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = true;
    }

    [RelayCommand]
    private void CancelDangerAction()
    {
        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = false;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmClearBirdRecords))]
    private async Task ConfirmClearBirdRecordsAsync(CancellationToken cancellationToken)
    {
        await ExecuteDangerActionAsync(async token =>
        {
            await _birdManager.FlushPendingOperationsAsync(token);
            var removed = await _databaseMaintenanceService.ClearBirdRecordsAsync(token);
            _birdManager.Store.ReplaceBirds(Array.Empty<BirdDTO>());
            _birdManager.Store.CompleteLoading();
            _autoExportCoordinator.MarkDirty();
            IsConfirmingClearBirdRecords = false;
            _notificationService.ShowSuccessLocalized("Info.BirdRecordsCleared", removed);
        }, "Error.CannotClearBirdRecords", cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanConfirmResetLocalDatabase))]
    private async Task ConfirmResetLocalDatabaseAsync(CancellationToken cancellationToken)
    {
        await ExecuteDangerActionAsync(async token =>
        {
            await _birdManager.FlushPendingOperationsAsync(token);
            await _databaseMaintenanceService.ResetLocalDatabaseAsync(token);
            _birdManager.Store.ReplaceBirds(Array.Empty<BirdDTO>());
            _birdManager.Store.CompleteLoading();
            _autoExportCoordinator.MarkDirty();
            IsConfirmingResetLocalDatabase = false;
            _notificationService.ShowSuccessLocalized("Info.LocalDatabaseReset");
        }, "Error.CannotResetLocalDatabase", cancellationToken);
    }

    partial void OnIsDangerZoneBusyChanged(bool value)
    {
        UpdateChildExternalBusy();
    }

    private async Task ExecuteDangerActionAsync(
        Func<CancellationToken, Task> action,
        string errorKey,
        CancellationToken cancellationToken)
    {
        IsDangerZoneBusy = true;
        try
        {
            await action(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized(errorKey);
        }
        finally
        {
            IsDangerZoneBusy = false;
        }
    }

    private bool CanStartDangerAction()
    {
        return !ImportExportSettings.IsTransferBusy && !IsDangerZoneBusy;
    }

    private bool CanConfirmClearBirdRecords()
    {
        return IsConfirmingClearBirdRecords && CanStartDangerAction();
    }

    private bool CanConfirmResetLocalDatabase()
    {
        return SupportsLocalDatabaseReset && IsConfirmingResetLocalDatabase && CanStartDangerAction();
    }

    private void OnSyncConfirmationStarted(object? sender, EventArgs e)
    {
        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = false;
    }

    private void OnImportExportSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImportExportSettingsViewModel.IsTransferBusy))
        {
            BeginClearBirdRecordsCommand.NotifyCanExecuteChanged();
            BeginResetLocalDatabaseCommand.NotifyCanExecuteChanged();
            ConfirmClearBirdRecordsCommand.NotifyCanExecuteChanged();
            ConfirmResetLocalDatabaseCommand.NotifyCanExecuteChanged();
            UpdateChildExternalBusy();
        }
    }

    private void UpdateChildExternalBusy()
    {
        ImportExportSettings.SetExternalBusy(IsDangerZoneBusy);
        SyncSettings.SetExternalBusy(ImportExportSettings.IsTransferBusy || IsDangerZoneBusy);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _importExportSettings.PropertyChanged -= OnImportExportSettingsPropertyChanged;
        _syncSettings.SyncConfirmationStarted -= OnSyncConfirmationStarted;
        _appearanceSettings.Dispose();
        _importExportSettings.Dispose();
        _syncSettings.Dispose();
        _disposed = true;
    }
}
