using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly AppearanceSettingsViewModel _appearanceSettings;
    private readonly DangerZoneSettingsViewModel _dangerZoneSettings;
    private readonly ImportExportSettingsViewModel _importExportSettings;
    private readonly SyncSettingsViewModel _syncSettings;
    private bool _disposed;

    public SettingsViewModel(AppearanceSettingsViewModel appearanceSettings,
        ImportExportSettingsViewModel importExportSettings,
        SyncSettingsViewModel syncSettings,
        DangerZoneSettingsViewModel dangerZoneSettings)
    {
        _appearanceSettings = appearanceSettings;
        _importExportSettings = importExportSettings;
        _syncSettings = syncSettings;
        _dangerZoneSettings = dangerZoneSettings;

        _importExportSettings.PropertyChanged += OnImportExportSettingsPropertyChanged;
        _syncSettings.SyncConfirmationStarted += OnSyncConfirmationStarted;
        _dangerZoneSettings.PropertyChanged += OnDangerZoneSettingsPropertyChanged;
        _dangerZoneSettings.DangerConfirmationStarted += OnDangerConfirmationStarted;
        UpdateChildExternalBusy();
    }

    public AppearanceSettingsViewModel AppearanceSettings => _appearanceSettings;

    public ImportExportSettingsViewModel ImportExportSettings => _importExportSettings;

    public SyncSettingsViewModel SyncSettings => _syncSettings;

    public DangerZoneSettingsViewModel DangerZoneSettings => _dangerZoneSettings;

    private void OnSyncConfirmationStarted(object? sender, EventArgs e)
    {
        DangerZoneSettings.CancelPendingConfirmations();
    }

    private void OnDangerConfirmationStarted(object? sender, EventArgs e)
    {
        SyncSettings.CancelPendingConfirmations();
    }

    private void OnImportExportSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImportExportSettingsViewModel.IsTransferBusy))
        {
            UpdateChildExternalBusy();
        }
    }

    private void OnDangerZoneSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DangerZoneSettingsViewModel.IsDangerZoneBusy))
            UpdateChildExternalBusy();
    }

    private void UpdateChildExternalBusy()
    {
        DangerZoneSettings.SetExternalBusy(ImportExportSettings.IsTransferBusy);
        ImportExportSettings.SetExternalBusy(DangerZoneSettings.IsDangerZoneBusy);
        SyncSettings.SetExternalBusy(ImportExportSettings.IsTransferBusy || DangerZoneSettings.IsDangerZoneBusy);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _importExportSettings.PropertyChanged -= OnImportExportSettingsPropertyChanged;
        _syncSettings.SyncConfirmationStarted -= OnSyncConfirmationStarted;
        _dangerZoneSettings.PropertyChanged -= OnDangerZoneSettingsPropertyChanged;
        _dangerZoneSettings.DangerConfirmationStarted -= OnDangerConfirmationStarted;
        _appearanceSettings.Dispose();
        _importExportSettings.Dispose();
        _syncSettings.Dispose();
        _dangerZoneSettings.Dispose();
        _disposed = true;
    }
}
