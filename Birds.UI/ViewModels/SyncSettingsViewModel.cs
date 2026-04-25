using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Birds.Shared.Localization;
using Birds.Shared.Sync;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels;

public partial class SyncSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IAutoExportCoordinator _autoExportCoordinator;
    private readonly IBirdManager _birdManager;
    private readonly ILocalizationService _localization;
    private readonly INotificationService _notificationService;
    private readonly IAppPreferencesService _preferences;
    private readonly IRemoteSyncController _remoteSyncController;
    private readonly IRemoteSyncStatusSource _remoteSyncStatus;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _disposed;
    private bool _isSynchronizingSelections;
    private CancellationTokenSource? _syncControlCancellation;

    private ReadOnlyCollection<SyncIntervalOption> _availableSyncIntervals =
        new(new List<SyncIntervalOption>());

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SyncNowCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleRemoteSyncPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginUploadLocalSnapshotToRemoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmUploadLocalSnapshotToRemoteCommand))]
    private bool isExternalBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemoteSyncRecentActivityToggleLabel))]
    private bool isRemoteSyncRecentActivityExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoteSnapshotConfirmationVisible))]
    [NotifyCanExecuteChangedFor(nameof(BeginRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRedownloadRemoteSnapshotCommand))]
    private bool isConfirmingRedownloadRemoteSnapshot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoteUploadConfirmationVisible))]
    [NotifyCanExecuteChangedFor(nameof(BeginUploadLocalSnapshotToRemoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmUploadLocalSnapshotToRemoteCommand))]
    private bool isConfirmingUploadLocalSnapshotToRemote;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SyncNowCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleRemoteSyncPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginUploadLocalSnapshotToRemoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmUploadLocalSnapshotToRemoteCommand))]
    private bool isSyncControlBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncIntervalHint))]
    private string selectedSyncInterval = AppPreferencesState.DefaultSyncInterval;

    [ObservableProperty] private SyncIntervalOption? selectedSyncIntervalOption;

    public SyncSettingsViewModel(
        IAppPreferencesService preferences,
        ILocalizationService localization,
        IBirdManager birdManager,
        IAutoExportCoordinator autoExportCoordinator,
        INotificationService notificationService,
        IRemoteSyncStatusSource remoteSyncStatus,
        IRemoteSyncController remoteSyncController)
    {
        _preferences = preferences;
        _localization = localization;
        _birdManager = birdManager;
        _autoExportCoordinator = autoExportCoordinator;
        _notificationService = notificationService;
        _remoteSyncStatus = remoteSyncStatus;
        _remoteSyncController = remoteSyncController;

        BuildAvailableSyncIntervals();
        ReloadFromPreferences();

        _preferences.PropertyChanged += OnPreferencesChanged;
        _localization.LanguageChanged += OnLanguageChanged;
        _remoteSyncStatus.PropertyChanged += OnRemoteSyncStatusChanged;
        _birdManager.Store.Birds.CollectionChanged += OnBirdStoreCollectionChanged;
    }

    public event EventHandler? SyncConfirmationStarted;

    public ReadOnlyCollection<SyncIntervalOption> AvailableSyncIntervals
    {
        get => _availableSyncIntervals;
        private set => SetProperty(ref _availableSyncIntervals, value);
    }

    public string SyncIntervalHint => _localization.GetString(
        "Settings.SyncIntervalHint",
        AvailableSyncIntervals.FirstOrDefault(x => x.Code == SelectedSyncInterval)?.DisplayName
        ?? _localization.GetString("Settings.SyncIntervalOption.TenSeconds"));

    public RemoteSyncDisplayState RemoteSyncStatus => _remoteSyncStatus.Status;

    public string RemoteSyncStatusLabel => RemoteSyncStatusTextFormatter.GetLabel(_localization, RemoteSyncStatus);

    public string RemoteSyncStatusHint => RemoteSyncStatusTextFormatter.GetHint(_localization, _remoteSyncStatus);

    public bool IsRemoteSyncEnabled => _remoteSyncController.IsEnabled;

    public bool IsRemoteSyncConfigured => _remoteSyncController.IsConfigured;

    public string? RemoteSyncConfigurationErrorMessage => _remoteSyncController.ConfigurationErrorMessage;

    public bool IsRemoteSyncPaused => RemoteSyncStatus == RemoteSyncDisplayState.Paused;

    public bool IsRemoteSyncSyncing => RemoteSyncStatus == RemoteSyncDisplayState.Syncing;

    public bool IsRemoteUploadConfirmationVisible => IsConfirmingUploadLocalSnapshotToRemote;

    public int RemoteSyncPendingOperationCount => _remoteSyncStatus.PendingOperationCount;

    public string RemoteSyncPendingCountLabel => _localization.GetString("Settings.SyncMeta.PendingLabel");

    public string RemoteSyncPendingCountValue => RemoteSyncPendingOperationCount == 0
        ? _localization.GetString("Settings.SyncMeta.PendingNone")
        : RemoteSyncPendingOperationCount.ToString(_localization.CurrentCulture);

    public string RemoteSyncLastSuccessfulSyncLabel => _localization.GetString("Settings.SyncMeta.LastSuccessLabel");

    public string RemoteSyncLastSuccessfulSyncValue => _remoteSyncStatus.LastSuccessfulSyncAtUtc.HasValue
        ? _localization.FormatDateTime(ToLocalTime(_remoteSyncStatus.LastSuccessfulSyncAtUtc.Value))
        : _localization.GetString("Settings.SyncMeta.Never");

    public string RemoteSnapshotStateLabel => _localization.GetString("Settings.SyncMeta.RemoteStateLabel");

    public string RemoteSnapshotStateValue => _remoteSyncStatus.RemoteSnapshotState switch
    {
        RemoteSyncSnapshotState.Empty => _localization.GetString("Settings.SyncMeta.RemoteStateEmpty"),
        RemoteSyncSnapshotState.HasData when _remoteSyncStatus.RemoteBirdCount.HasValue
            => _localization.GetString("Settings.SyncMeta.RemoteStateHasDataCount", _remoteSyncStatus.RemoteBirdCount.Value),
        RemoteSyncSnapshotState.HasData => _localization.GetString("Settings.SyncMeta.RemoteStateHasData"),
        _ => _localization.GetString("Settings.SyncMeta.RemoteStateUnknown")
    };

    public bool IsRemoteSnapshotEmptyWarningVisible => IsRemoteSyncConfigured
                                                       && _remoteSyncStatus.RemoteSnapshotState == RemoteSyncSnapshotState.Empty
                                                       && _birdManager.Store.Birds.Count > 0;

    public string RemoteSyncRecentActivityLabel => _localization.GetString("Settings.SyncMeta.RecentActivityLabel");

    public string RemoteSyncRecentActivityEmpty => _localization.GetString("Settings.SyncMeta.RecentActivityEmpty");

    public string RemoteSyncRecentActivityToggleLabel => IsRemoteSyncRecentActivityExpanded
        ? _localization.GetString("Settings.SyncMeta.RecentActivityCollapse")
        : _localization.GetString("Settings.SyncMeta.RecentActivityExpand");

    public bool HasRemoteSyncRecentActivity => _remoteSyncStatus.RecentActivity.Count > 0;

    public IReadOnlyList<RemoteSyncActivityDisplayItem> RemoteSyncRecentActivityItems
        => _remoteSyncStatus.RecentActivity
            .Select(CreateRemoteSyncActivityDisplayItem)
            .ToArray();

    public string RemoteSyncPauseActionLabel => IsRemoteSyncPaused
        ? _localization.GetString("Settings.SyncAction.Resume")
        : _localization.GetString("Settings.SyncAction.Pause");

    public bool IsRemoteSnapshotConfirmationVisible => IsConfirmingRedownloadRemoteSnapshot;

    public void SetExternalBusy(bool value)
    {
        IsExternalBusy = value;
    }

    public void CancelPendingConfirmations()
    {
        IsConfirmingRedownloadRemoteSnapshot = false;
        IsConfirmingUploadLocalSnapshotToRemote = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _syncControlCancellation?.Cancel();
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _localization.LanguageChanged -= OnLanguageChanged;
        _remoteSyncStatus.PropertyChanged -= OnRemoteSyncStatusChanged;
        _birdManager.Store.Birds.CollectionChanged -= OnBirdStoreCollectionChanged;
        _lifetimeCancellation.Dispose();
    }

    [RelayCommand]
    private void ToggleRemoteSyncRecentActivity()
    {
        IsRemoteSyncRecentActivityExpanded = !IsRemoteSyncRecentActivityExpanded;
    }

    [RelayCommand(CanExecute = nameof(CanSyncNow))]
    private async Task SyncNowAsync(CancellationToken cancellationToken)
    {
        var operationCancellation = CreateSyncControlCancellation(cancellationToken);
        IsSyncControlBusy = true;
        try
        {
            var operationToken = operationCancellation.Token;
            await _remoteSyncController.SyncNowAsync(operationToken);

            if (!IsRemoteSyncConfigured && !_disposed)
                ShowRemoteSyncConfigurationIssue();
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // User canceled or application is shutting down.
        }
        finally
        {
            if (!_disposed)
                IsSyncControlBusy = false;

            ClearSyncControlCancellation(operationCancellation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleRemoteSyncPause))]
    private async Task ToggleRemoteSyncPauseAsync(CancellationToken cancellationToken)
    {
        if (!IsRemoteSyncConfigured)
            return;

        var operationCancellation = CreateSyncControlCancellation(cancellationToken);
        IsSyncControlBusy = true;
        try
        {
            if (IsRemoteSyncPaused)
                await _remoteSyncController.ResumeAsync(operationCancellation.Token);
            else
                await _remoteSyncController.PauseAsync(operationCancellation.Token);
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // User canceled or application is shutting down.
        }
        finally
        {
            if (!_disposed)
                IsSyncControlBusy = false;

            ClearSyncControlCancellation(operationCancellation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanBeginUploadLocalSnapshotToRemote))]
    private void BeginUploadLocalSnapshotToRemote()
    {
        IsConfirmingRedownloadRemoteSnapshot = false;
        IsConfirmingUploadLocalSnapshotToRemote = true;
        SyncConfirmationStarted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CancelUploadLocalSnapshotToRemote()
    {
        IsConfirmingUploadLocalSnapshotToRemote = false;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmUploadLocalSnapshotToRemote))]
    private async Task ConfirmUploadLocalSnapshotToRemoteAsync(CancellationToken cancellationToken)
    {
        if (!IsRemoteSyncConfigured)
            return;

        var operationCancellation = CreateSyncControlCancellation(cancellationToken);
        IsSyncControlBusy = true;
        try
        {
            var operationToken = operationCancellation.Token;
            await _birdManager.FlushPendingOperationsAsync(operationToken);
            var uploaded = await _remoteSyncController.UploadLocalSnapshotToRemoteAsync(operationToken);
            if (!uploaded)
            {
                if (_disposed)
                    return;

                _notificationService.ShowErrorLocalized("Error.CannotUploadLocalSnapshotToRemote");
                return;
            }

            if (_disposed)
                return;

            IsConfirmingUploadLocalSnapshotToRemote = false;
            _notificationService.ShowSuccessLocalized("Info.RemoteSnapshotUploaded");
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.CannotUploadLocalSnapshotToRemote");
        }
        finally
        {
            if (!_disposed)
                IsSyncControlBusy = false;

            ClearSyncControlCancellation(operationCancellation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanBeginRedownloadRemoteSnapshot))]
    private void BeginRedownloadRemoteSnapshot()
    {
        IsConfirmingUploadLocalSnapshotToRemote = false;
        IsConfirmingRedownloadRemoteSnapshot = true;
        SyncConfirmationStarted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CancelRedownloadRemoteSnapshot()
    {
        IsConfirmingRedownloadRemoteSnapshot = false;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmRedownloadRemoteSnapshot))]
    private async Task ConfirmRedownloadRemoteSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!IsRemoteSyncConfigured)
            return;

        var operationCancellation = CreateSyncControlCancellation(cancellationToken);
        IsSyncControlBusy = true;
        try
        {
            var operationToken = operationCancellation.Token;
            await _birdManager.FlushPendingOperationsAsync(operationToken);
            var restored = await _remoteSyncController.RedownloadRemoteSnapshotAsync(operationToken);
            if (!restored)
            {
                if (_disposed)
                    return;

                _notificationService.ShowErrorLocalized("Error.CannotRedownloadRemoteSnapshot");
                return;
            }

            await _birdManager.ReloadAsync(operationToken);
            if (_disposed)
                return;

            _autoExportCoordinator.MarkDirty();
            IsConfirmingRedownloadRemoteSnapshot = false;
            _notificationService.ShowSuccessLocalized("Info.RemoteSnapshotRedownloaded");
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.CannotRedownloadRemoteSnapshot");
        }
        finally
        {
            if (!_disposed)
                IsSyncControlBusy = false;

            ClearSyncControlCancellation(operationCancellation);
        }
    }

    partial void OnSelectedSyncIntervalChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            RestoreSelectedSyncIntervalFromPreferences();
            return;
        }

        var normalized = RemoteSyncIntervalPresets.Normalize(value);

        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(SyncIntervalHint));
            return;
        }

        if (_preferences.SelectedSyncInterval != normalized)
            _preferences.SelectedSyncInterval = normalized;

        OnPropertyChanged(nameof(SyncIntervalHint));
    }

    partial void OnSelectedSyncIntervalOptionChanged(SyncIntervalOption? value)
    {
        if (_isSynchronizingSelections)
            return;

        if (value is null)
        {
            RestoreSelectedSyncIntervalFromPreferences();
            return;
        }

        SelectedSyncInterval = value.Code;
    }

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppPreferencesService.SelectedSyncInterval))
            ReloadFromPreferences();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        BuildAvailableSyncIntervals();
        ReloadFromPreferences();
        OnPropertyChanged(nameof(AvailableSyncIntervals));
        OnPropertyChanged(nameof(SelectedSyncInterval));
        OnPropertyChanged(nameof(SelectedSyncIntervalOption));
        OnPropertyChanged(nameof(SyncIntervalHint));
        OnPropertyChanged(nameof(RemoteSyncStatusLabel));
        OnPropertyChanged(nameof(RemoteSyncStatusHint));
        OnPropertyChanged(nameof(RemoteSyncPendingCountLabel));
        OnPropertyChanged(nameof(RemoteSyncPendingCountValue));
        OnPropertyChanged(nameof(RemoteSyncLastSuccessfulSyncLabel));
        OnPropertyChanged(nameof(RemoteSyncLastSuccessfulSyncValue));
        OnPropertyChanged(nameof(RemoteSnapshotStateLabel));
        OnPropertyChanged(nameof(RemoteSnapshotStateValue));
        OnPropertyChanged(nameof(IsRemoteSnapshotEmptyWarningVisible));
        OnPropertyChanged(nameof(RemoteSyncRecentActivityLabel));
        OnPropertyChanged(nameof(RemoteSyncRecentActivityEmpty));
        OnPropertyChanged(nameof(RemoteSyncRecentActivityToggleLabel));
        OnPropertyChanged(nameof(HasRemoteSyncRecentActivity));
        OnPropertyChanged(nameof(RemoteSyncRecentActivityItems));
        OnPropertyChanged(nameof(RemoteSyncPauseActionLabel));
    }

    private void BuildAvailableSyncIntervals()
    {
        AvailableSyncIntervals = CreateLocalizedOptions(
            [
                (RemoteSyncIntervalPresets.FiveSeconds, _localization.GetString("Settings.SyncIntervalOption.FiveSeconds")),
                (RemoteSyncIntervalPresets.TenSeconds, _localization.GetString("Settings.SyncIntervalOption.TenSeconds")),
                (RemoteSyncIntervalPresets.ThirtySeconds, _localization.GetString("Settings.SyncIntervalOption.ThirtySeconds")),
                (RemoteSyncIntervalPresets.OneMinute, _localization.GetString("Settings.SyncIntervalOption.OneMinute"))
            ],
            static (code, displayName) => new SyncIntervalOption(code, displayName));
    }

    private void ReloadFromPreferences()
    {
        var normalizedSyncInterval = RemoteSyncIntervalPresets.Normalize(_preferences.SelectedSyncInterval);

        _isSynchronizingSelections = true;
        try
        {
            SelectedSyncInterval = normalizedSyncInterval;
            SelectedSyncIntervalOption = AvailableSyncIntervals.FirstOrDefault(
                option => string.Equals(option.Code, normalizedSyncInterval, StringComparison.Ordinal));
        }
        finally
        {
            _isSynchronizingSelections = false;
        }

        OnPropertyChanged(nameof(SyncIntervalHint));
    }

    private void RestoreSelectedSyncIntervalFromPreferences()
    {
        var normalized = RemoteSyncIntervalPresets.Normalize(_preferences.SelectedSyncInterval);
        _isSynchronizingSelections = true;
        try
        {
            SelectedSyncInterval = normalized;
            SelectedSyncIntervalOption = AvailableSyncIntervals.FirstOrDefault(
                option => string.Equals(option.Code, normalized, StringComparison.Ordinal));
        }
        finally
        {
            _isSynchronizingSelections = false;
        }

        OnPropertyChanged(nameof(SelectedSyncInterval));
        OnPropertyChanged(nameof(SelectedSyncIntervalOption));
        OnPropertyChanged(nameof(SyncIntervalHint));
    }

    private bool CanSyncNow()
    {
        return IsRemoteSyncEnabled
               && !IsExternalBusy
               && !IsSyncControlBusy
               && !IsRemoteSyncSyncing;
    }

    private bool CanToggleRemoteSyncPause()
    {
        return IsRemoteSyncConfigured
               && !IsExternalBusy
               && !IsSyncControlBusy;
    }

    private bool CanBeginRedownloadRemoteSnapshot()
    {
        return IsRemoteSyncConfigured
               && !IsExternalBusy
               && !IsSyncControlBusy;
    }

    private bool CanConfirmRedownloadRemoteSnapshot()
    {
        return IsConfirmingRedownloadRemoteSnapshot && CanBeginRedownloadRemoteSnapshot();
    }

    private bool CanBeginUploadLocalSnapshotToRemote()
    {
        return IsRemoteSyncConfigured
               && !IsExternalBusy
               && !IsSyncControlBusy;
    }

    private bool CanConfirmUploadLocalSnapshotToRemote()
    {
        return IsConfirmingUploadLocalSnapshotToRemote && CanBeginUploadLocalSnapshotToRemote();
    }

    private CancellationTokenSource CreateSyncControlCancellation(CancellationToken cancellationToken)
    {
        var previous = _syncControlCancellation;
        var current = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        _syncControlCancellation = current;

        previous?.Cancel();
        previous?.Dispose();

        return current;
    }

    private void ClearSyncControlCancellation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_syncControlCancellation, operationCancellation))
            _syncControlCancellation = null;

        operationCancellation.Dispose();
    }

    private void OnRemoteSyncStatusChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IRemoteSyncStatusSource.Status)
            or nameof(IRemoteSyncStatusSource.RemoteSnapshotState)
            or nameof(IRemoteSyncStatusSource.RemoteBirdCount)
            or nameof(IRemoteSyncStatusSource.LastSuccessfulSyncAtUtc)
            or nameof(IRemoteSyncStatusSource.LastAttemptAtUtc)
            or nameof(IRemoteSyncStatusSource.LastErrorMessage)
            or nameof(IRemoteSyncStatusSource.LastProcessedCount)
            or nameof(IRemoteSyncStatusSource.PendingOperationCount)
            or nameof(IRemoteSyncStatusSource.RecentActivity))
        {
            OnPropertyChanged(nameof(RemoteSyncStatus));
            OnPropertyChanged(nameof(RemoteSyncStatusLabel));
            OnPropertyChanged(nameof(RemoteSyncStatusHint));
            OnPropertyChanged(nameof(IsRemoteSyncEnabled));
            OnPropertyChanged(nameof(IsRemoteSyncConfigured));
            OnPropertyChanged(nameof(RemoteSyncConfigurationErrorMessage));
            OnPropertyChanged(nameof(IsRemoteSyncPaused));
            OnPropertyChanged(nameof(IsRemoteSyncSyncing));
            OnPropertyChanged(nameof(IsRemoteUploadConfirmationVisible));
            OnPropertyChanged(nameof(RemoteSyncPendingOperationCount));
            OnPropertyChanged(nameof(RemoteSyncPendingCountValue));
            OnPropertyChanged(nameof(RemoteSyncLastSuccessfulSyncValue));
            OnPropertyChanged(nameof(RemoteSnapshotStateValue));
            OnPropertyChanged(nameof(IsRemoteSnapshotEmptyWarningVisible));
            OnPropertyChanged(nameof(HasRemoteSyncRecentActivity));
            OnPropertyChanged(nameof(RemoteSyncRecentActivityItems));
            OnPropertyChanged(nameof(RemoteSyncPauseActionLabel));
            SyncNowCommand.NotifyCanExecuteChanged();
            ToggleRemoteSyncPauseCommand.NotifyCanExecuteChanged();
            BeginRedownloadRemoteSnapshotCommand.NotifyCanExecuteChanged();
            ConfirmRedownloadRemoteSnapshotCommand.NotifyCanExecuteChanged();
            BeginUploadLocalSnapshotToRemoteCommand.NotifyCanExecuteChanged();
            ConfirmUploadLocalSnapshotToRemoteCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnBirdStoreCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsRemoteSnapshotEmptyWarningVisible));
    }

    private RemoteSyncActivityDisplayItem CreateRemoteSyncActivityDisplayItem(RemoteSyncActivityEntry entry)
    {
        var title = RemoteSyncStatusTextFormatter.GetLabel(_localization, entry.Status);
        var description = entry.Status switch
        {
            RemoteSyncDisplayState.Synced when entry.ProcessedCount > 0
                => _localization.GetString("Settings.SyncRecent.SyncedProcessed", entry.ProcessedCount),
            RemoteSyncDisplayState.Synced
                => _localization.GetString("Settings.SyncRecent.SyncedIdle"),
            RemoteSyncDisplayState.Paused
                => _localization.GetString("Settings.SyncRecent.Paused"),
            RemoteSyncDisplayState.Offline
                => string.IsNullOrWhiteSpace(entry.ErrorMessage)
                    ? _localization.GetString("Settings.SyncRecent.Offline")
                    : entry.ErrorMessage!,
            RemoteSyncDisplayState.Error
                => string.IsNullOrWhiteSpace(entry.ErrorMessage)
                    ? _localization.GetString("Settings.SyncRecent.Error")
                    : entry.ErrorMessage!,
            _ => _localization.GetString("Settings.SyncRecent.LocalOnly")
        };

        return new RemoteSyncActivityDisplayItem(
            title,
            description,
            _localization.FormatDateTime(ToLocalTime(entry.OccurredAtUtc)),
            entry.Status);
    }

    private static DateTime ToLocalTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return utc.ToLocalTime();
    }

    private void ShowRemoteSyncConfigurationIssue()
    {
        if (string.IsNullOrWhiteSpace(RemoteSyncConfigurationErrorMessage))
            return;

        _notificationService.ShowWarning(RemoteSyncConfigurationErrorMessage);
    }

    private static ReadOnlyCollection<TOption> CreateLocalizedOptions<TOption>(
        IReadOnlyList<(string Code, string DisplayName)> entries,
        Func<string, string, TOption> factory)
        where TOption : class
    {
        return new ReadOnlyCollection<TOption>(
            entries.Select(entry => factory(entry.Code, entry.DisplayName)).ToList());
    }
}
