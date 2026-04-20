using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using Birds.Application.Commands.ImportBirds;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Shared.Localization;
using Birds.Shared.Sync;
using Birds.UI.Services.Dialogs.Interfaces;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Import;
using Birds.UI.Services.Import.Interfaces;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Shell.Interfaces;
using Birds.UI.Services.Sync;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Birds.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAutoExportCoordinator _autoExportCoordinator;
    private readonly IBirdManager _birdManager;
    private readonly IDatabaseMaintenanceService _databaseMaintenanceService;
    private readonly IDataFileDialogService _dataFileDialogService;
    private readonly IExportPathProvider _exportPathProvider;
    private readonly IExportService _exportService;
    private readonly IImportService _importService;
    private readonly ILocalizationService _localization;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly IPathNavigationService _pathNavigationService;
    private readonly IAppPreferencesService _preferences;
    private readonly IRemoteSyncController _remoteSyncController;
    private readonly IRemoteSyncStatusSource _remoteSyncStatus;
    private readonly IThemeService _themeService;

    private ReadOnlyCollection<DateFormatOption> _availableDateFormats =
        new(new List<DateFormatOption>());

    private ReadOnlyCollection<ImportModeOption> _availableImportModes =
        new(new List<ImportModeOption>());

    private ReadOnlyCollection<LanguageOption> _availableLanguages =
        new(new List<LanguageOption>());

    private ReadOnlyCollection<ThemeOption> _availableThemes =
        new(new List<ThemeOption>());

    private ReadOnlyCollection<SyncIntervalOption> _availableSyncIntervals =
        new(new List<SyncIntervalOption>());

    private bool _isSynchronizingSelections;

    [ObservableProperty] private bool autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDangerConfirmationVisible))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
    private bool isConfirmingClearBirdRecords;

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
    [NotifyPropertyChangedFor(nameof(IsDangerConfirmationVisible))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
    private bool isConfirmingResetLocalDatabase;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginResetLocalDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(SyncNowCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleRemoteSyncPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRedownloadRemoteSnapshotCommand))]
    private bool isDangerZoneBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginResetLocalDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRedownloadRemoteSnapshotCommand))]
    private bool isDataTransferBusy;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(RemoteSyncRecentActivityToggleLabel))]
    private bool isRemoteSyncRecentActivityExpanded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SyncNowCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleRemoteSyncPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRedownloadRemoteSnapshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginUploadLocalSnapshotToRemoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmUploadLocalSnapshotToRemoteCommand))]
    private bool isSyncControlBusy;

    [ObservableProperty] private string selectedDateFormat = AppPreferencesState.DefaultDateFormat;

    [ObservableProperty] private string selectedImportMode = AppPreferencesState.DefaultImportMode;

    [ObservableProperty] private string selectedSyncInterval = AppPreferencesState.DefaultSyncInterval;

    [ObservableProperty] private string selectedLanguage = AppPreferencesState.DefaultLanguage;

    [ObservableProperty] private string selectedTheme = AppPreferencesState.DefaultTheme;

    [ObservableProperty] private bool showNotificationBadge = true;

    [ObservableProperty] private bool showSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;

    public SettingsViewModel(IAppPreferencesService preferences,
        IThemeService themeService,
        ILocalizationService localization,
        IBirdManager birdManager,
        IExportService exportService,
        IExportPathProvider exportPathProvider,
        IAutoExportCoordinator autoExportCoordinator,
        IImportService importService,
        IDataFileDialogService dataFileDialogService,
        IPathNavigationService pathNavigationService,
        INotificationService notificationService,
        IMediator mediator,
        IDatabaseMaintenanceService databaseMaintenanceService,
        IRemoteSyncStatusSource remoteSyncStatus,
        IRemoteSyncController remoteSyncController)
    {
        _preferences = preferences;
        _themeService = themeService;
        _localization = localization;
        _birdManager = birdManager;
        _exportService = exportService;
        _exportPathProvider = exportPathProvider;
        _autoExportCoordinator = autoExportCoordinator;
        _importService = importService;
        _dataFileDialogService = dataFileDialogService;
        _pathNavigationService = pathNavigationService;
        _notificationService = notificationService;
        _mediator = mediator;
        _databaseMaintenanceService = databaseMaintenanceService;
        _remoteSyncStatus = remoteSyncStatus;
        _remoteSyncController = remoteSyncController;

        BuildAvailableLanguages();
        BuildAvailableThemes();
        BuildAvailableDateFormats();
        BuildAvailableImportModes();
        BuildAvailableSyncIntervals();
        ReloadFromPreferences();

        _preferences.PropertyChanged += OnPreferencesChanged;
        _localization.LanguageChanged += OnLanguageChanged;
        _remoteSyncStatus.PropertyChanged += OnRemoteSyncStatusChanged;
        _birdManager.Store.Birds.CollectionChanged += OnBirdStoreCollectionChanged;
    }

    public ReadOnlyCollection<LanguageOption> AvailableLanguages
    {
        get => _availableLanguages;
        private set => SetProperty(ref _availableLanguages, value);
    }

    public ReadOnlyCollection<ThemeOption> AvailableThemes
    {
        get => _availableThemes;
        private set => SetProperty(ref _availableThemes, value);
    }

    public ReadOnlyCollection<DateFormatOption> AvailableDateFormats
    {
        get => _availableDateFormats;
        private set => SetProperty(ref _availableDateFormats, value);
    }

    public ReadOnlyCollection<ImportModeOption> AvailableImportModes
    {
        get => _availableImportModes;
        private set => SetProperty(ref _availableImportModes, value);
    }

    public ReadOnlyCollection<SyncIntervalOption> AvailableSyncIntervals
    {
        get => _availableSyncIntervals;
        private set => SetProperty(ref _availableSyncIntervals, value);
    }

    public string LanguageHint =>
        SelectedLanguage == AppLanguages.Russian
            ? _localization.GetString("Settings.LanguageHint.Russian")
            : _localization.GetString("Settings.LanguageHint.English");

    public string ThemeHint =>
        SelectedTheme == ThemeKeys.Graphite
            ? _localization.GetString("Settings.ThemeHint.Graphite")
            : _localization.GetString("Settings.ThemeHint.Steel");

    public string DateFormatHint =>
        SelectedDateFormat switch
        {
            DateDisplayFormats.MonthDayYear => _localization.GetString("Settings.DateFormatHint.MonthDayYear"),
            DateDisplayFormats.YearMonthDay => _localization.GetString("Settings.DateFormatHint.YearMonthDay"),
            _ => _localization.GetString("Settings.DateFormatHint.DayMonthYear")
        };

    public string NotificationsHint =>
        ShowNotificationBadge
            ? _localization.GetString("Settings.NotificationsHint.Enabled")
            : _localization.GetString("Settings.NotificationsHint.Disabled");

    public string SyncIndicatorHint =>
        ShowSyncStatusIndicator
            ? _localization.GetString("Settings.SyncIndicatorHint.Enabled")
            : _localization.GetString("Settings.SyncIndicatorHint.Disabled");

    public string ExportPathHint =>
        _localization.GetString("Settings.Data.ExportHint", ResolveExportPath());

    public string ImportHint =>
        SelectedImportMode == BirdImportModes.Replace
            ? _localization.GetString("Settings.Data.ImportHint.Replace")
            : _localization.GetString("Settings.Data.ImportHint.Merge");

    public string ImportModeHint =>
        SelectedImportMode == BirdImportModes.Replace
            ? _localization.GetString("Settings.ImportModeHint.Replace")
            : _localization.GetString("Settings.ImportModeHint.Merge");

    public string AutoExportHint =>
        AutoExportEnabled
            ? _localization.GetString("Settings.AutoExportHint.Enabled")
            : _localization.GetString("Settings.AutoExportHint.Disabled");

    public string SyncIntervalHint => _localization.GetString(
        "Settings.SyncIntervalHint",
        AvailableSyncIntervals.FirstOrDefault(x => x.Code == SelectedSyncInterval)?.DisplayName
        ?? _localization.GetString("Settings.SyncIntervalOption.TenSeconds"));

    public RemoteSyncDisplayState RemoteSyncStatus => _remoteSyncStatus.Status;

    public string RemoteSyncStatusLabel => RemoteSyncStatusTextFormatter.GetLabel(_localization, RemoteSyncStatus);

    public string RemoteSyncStatusHint => RemoteSyncStatusTextFormatter.GetHint(_localization, _remoteSyncStatus);

    public bool IsRemoteSyncConfigured => _remoteSyncController.IsConfigured;

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

    public bool SupportsLocalDatabaseReset => _databaseMaintenanceService.CanResetLocalDatabase;

    public bool IsDangerConfirmationVisible => IsConfirmingClearBirdRecords || IsConfirmingResetLocalDatabase;

    [RelayCommand]
    private void ResetPreferences()
    {
        _preferences.ResetToDefaults();
        ReloadFromPreferences();
    }

    [RelayCommand]
    private void ToggleRemoteSyncRecentActivity()
    {
        IsRemoteSyncRecentActivityExpanded = !IsRemoteSyncRecentActivityExpanded;
    }

    [RelayCommand(CanExecute = nameof(CanTransferData))]
    private void ChooseExportPath()
    {
        var selectedPath = _dataFileDialogService.PickExportPath(ResolveExportPath());

        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        _preferences.CustomExportPath = Path.GetFullPath(selectedPath);
        OnPropertyChanged(nameof(ExportPathHint));
    }

    [RelayCommand(CanExecute = nameof(CanTransferData))]
    private void OpenExportFolder()
    {
        var exportDirectory = Path.GetDirectoryName(ResolveExportPath());
        if (string.IsNullOrWhiteSpace(exportDirectory))
            exportDirectory = Environment.CurrentDirectory;

        if (_pathNavigationService.OpenDirectory(exportDirectory))
            return;

        _notificationService.ShowErrorLocalized("Error.CannotOpenExportFolder");
    }

    [RelayCommand(CanExecute = nameof(CanTransferData))]
    private void OpenExportFile()
    {
        if (_pathNavigationService.OpenFile(ResolveExportPath()))
            return;

        _notificationService.ShowErrorLocalized("Error.CannotOpenExportFile");
    }

    [RelayCommand(CanExecute = nameof(CanTransferData))]
    private async Task ExportDataAsync(CancellationToken cancellationToken)
    {
        var targetPath = ResolveExportPath();

        IsDataTransferBusy = true;
        try
        {
            await _birdManager.FlushPendingOperationsAsync(cancellationToken);
            var snapshot = _birdManager.Store.Birds.ToList();
            await _exportService.ExportAsync(snapshot, targetPath, cancellationToken);
            _notificationService.ShowSuccessLocalized("Info.ExportSucceeded", targetPath);
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.ExportFailed");
        }
        finally
        {
            IsDataTransferBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSyncNow))]
    private async Task SyncNowAsync(CancellationToken cancellationToken)
    {
        if (!IsRemoteSyncConfigured)
            return;

        IsSyncControlBusy = true;
        try
        {
            await _remoteSyncController.SyncNowAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        finally
        {
            IsSyncControlBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleRemoteSyncPause))]
    private async Task ToggleRemoteSyncPauseAsync(CancellationToken cancellationToken)
    {
        if (!IsRemoteSyncConfigured)
            return;

        IsSyncControlBusy = true;
        try
        {
            if (IsRemoteSyncPaused)
                await _remoteSyncController.ResumeAsync(cancellationToken);
            else
                await _remoteSyncController.PauseAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        finally
        {
            IsSyncControlBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanBeginUploadLocalSnapshotToRemote))]
    private void BeginUploadLocalSnapshotToRemote()
    {
        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = false;
        IsConfirmingRedownloadRemoteSnapshot = false;
        IsConfirmingUploadLocalSnapshotToRemote = true;
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

        IsSyncControlBusy = true;
        try
        {
            await _birdManager.FlushPendingOperationsAsync(cancellationToken);
            var uploaded = await _remoteSyncController.UploadLocalSnapshotToRemoteAsync(cancellationToken);
            if (!uploaded)
            {
                _notificationService.ShowErrorLocalized("Error.CannotUploadLocalSnapshotToRemote");
                return;
            }

            IsConfirmingUploadLocalSnapshotToRemote = false;
            _notificationService.ShowSuccessLocalized("Info.RemoteSnapshotUploaded");
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.CannotUploadLocalSnapshotToRemote");
        }
        finally
        {
            IsSyncControlBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanBeginRedownloadRemoteSnapshot))]
    private void BeginRedownloadRemoteSnapshot()
    {
        IsConfirmingClearBirdRecords = false;
        IsConfirmingResetLocalDatabase = false;
        IsConfirmingUploadLocalSnapshotToRemote = false;
        IsConfirmingRedownloadRemoteSnapshot = true;
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

        IsSyncControlBusy = true;
        try
        {
            await _birdManager.FlushPendingOperationsAsync(cancellationToken);
            var restored = await _remoteSyncController.RedownloadRemoteSnapshotAsync(cancellationToken);
            if (!restored)
            {
                _notificationService.ShowErrorLocalized("Error.CannotRedownloadRemoteSnapshot");
                return;
            }

            await _birdManager.ReloadAsync(cancellationToken);
            _autoExportCoordinator.MarkDirty();
            IsConfirmingRedownloadRemoteSnapshot = false;
            _notificationService.ShowSuccessLocalized("Info.RemoteSnapshotRedownloaded");
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.CannotRedownloadRemoteSnapshot");
        }
        finally
        {
            IsSyncControlBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
    private void BeginClearBirdRecords()
    {
        IsConfirmingRedownloadRemoteSnapshot = false;
        IsConfirmingResetLocalDatabase = false;
        IsConfirmingClearBirdRecords = true;
    }

    [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
    private void BeginResetLocalDatabase()
    {
        if (!SupportsLocalDatabaseReset)
            return;

        IsConfirmingRedownloadRemoteSnapshot = false;
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

    [RelayCommand(CanExecute = nameof(CanTransferData))]
    private async Task ImportDataAsync(CancellationToken cancellationToken)
    {
        var suggestedPath = ResolveExportPath();
        var sourcePath = _dataFileDialogService.PickImportPath(suggestedPath);

        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        IsDataTransferBusy = true;
        try
        {
            var importPayload = await _importService.ImportAsync(sourcePath, cancellationToken);
            if (!importPayload.IsSuccess)
            {
                _notificationService.ShowError(importPayload.Error ?? _localization.GetString("Error.ImportFailed"));
                return;
            }

            await _birdManager.FlushPendingOperationsAsync(cancellationToken);
            var importMode = BirdImportModes.ToCommandMode(SelectedImportMode);
            var importResult = await _mediator.Send(
                new ImportBirdsCommand(importPayload.Value!, importMode),
                cancellationToken);
            if (!importResult.IsSuccess)
            {
                _notificationService.ShowError(importResult.Error ?? _localization.GetString("Error.ImportFailed"));
                return;
            }

            _birdManager.Store.ReplaceBirds(importResult.Value!.Snapshot);
            _birdManager.Store.CompleteLoading();
            _autoExportCoordinator.MarkDirty();

            if (importMode == BirdImportMode.Replace)
                _notificationService.ShowSuccessLocalized(
                    "Info.ImportReplacedSucceeded",
                    importResult.Value.Imported,
                    importResult.Value.Added,
                    importResult.Value.Updated,
                    importResult.Value.Removed);
            else
                _notificationService.ShowSuccessLocalized(
                    "Info.ImportMergedSucceeded",
                    importResult.Value.Imported,
                    importResult.Value.Added,
                    importResult.Value.Updated);
        }
        catch (OperationCanceledException)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.ImportFailed");
        }
        finally
        {
            IsDataTransferBusy = false;
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        var normalized = AppLanguages.Normalize(value);

        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(LanguageHint));
            return;
        }

        if (_preferences.SelectedLanguage != normalized)
            _preferences.SelectedLanguage = normalized;

        if (_localization.ApplyLanguage(normalized))
            _ = _birdManager.ReloadAsync(CancellationToken.None);

        OnPropertyChanged(nameof(LanguageHint));
    }

    partial void OnSelectedThemeChanged(string value)
    {
        var normalized = ThemeKeys.Normalize(value);

        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(ThemeHint));
            return;
        }

        if (_preferences.SelectedTheme != normalized)
            _preferences.SelectedTheme = normalized;

        _themeService.ApplyTheme(normalized);
        OnPropertyChanged(nameof(ThemeHint));
    }

    partial void OnSelectedDateFormatChanged(string value)
    {
        var normalized = DateDisplayFormats.Normalize(value);

        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(DateFormatHint));
            return;
        }

        if (_preferences.SelectedDateFormat != normalized)
            _preferences.SelectedDateFormat = normalized;

        _localization.ApplyDateFormat(normalized);
        OnPropertyChanged(nameof(DateFormatHint));
    }

    partial void OnSelectedImportModeChanged(string value)
    {
        var normalized = BirdImportModes.Normalize(value);

        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(ImportModeHint));
            OnPropertyChanged(nameof(ImportHint));
            return;
        }

        if (_preferences.SelectedImportMode != normalized)
            _preferences.SelectedImportMode = normalized;

        OnPropertyChanged(nameof(ImportModeHint));
        OnPropertyChanged(nameof(ImportHint));
    }

    partial void OnSelectedSyncIntervalChanged(string value)
    {
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

    partial void OnAutoExportEnabledChanged(bool value)
    {
        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(AutoExportHint));
            return;
        }

        if (_preferences.AutoExportEnabled != value)
            _preferences.AutoExportEnabled = value;

        OnPropertyChanged(nameof(AutoExportHint));
    }

    partial void OnShowNotificationBadgeChanged(bool value)
    {
        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(NotificationsHint));
            return;
        }

        if (_preferences.ShowNotificationBadge != value)
            _preferences.ShowNotificationBadge = value;

        OnPropertyChanged(nameof(NotificationsHint));
    }

    partial void OnShowSyncStatusIndicatorChanged(bool value)
    {
        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(SyncIndicatorHint));
            return;
        }

        if (_preferences.ShowSyncStatusIndicator != value)
            _preferences.ShowSyncStatusIndicator = value;

        OnPropertyChanged(nameof(SyncIndicatorHint));
    }

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IAppPreferencesService.SelectedLanguage)
            or nameof(IAppPreferencesService.SelectedTheme)
            or nameof(IAppPreferencesService.SelectedDateFormat)
            or nameof(IAppPreferencesService.SelectedImportMode)
            or nameof(IAppPreferencesService.SelectedSyncInterval)
            or nameof(IAppPreferencesService.CustomExportPath)
            or nameof(IAppPreferencesService.AutoExportEnabled)
            or nameof(IAppPreferencesService.ShowNotificationBadge)
            or nameof(IAppPreferencesService.ShowSyncStatusIndicator))
            ReloadFromPreferences(true);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var preservedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);

        BuildAvailableLanguages();
        BuildAvailableThemes();
        BuildAvailableDateFormats();
        BuildAvailableImportModes();
        BuildAvailableSyncIntervals();
        ReloadFromPreferences();
        _themeService.ApplyTheme(preservedTheme);
        OnPropertyChanged(nameof(AvailableLanguages));
        OnPropertyChanged(nameof(AvailableThemes));
        OnPropertyChanged(nameof(AvailableDateFormats));
        OnPropertyChanged(nameof(AvailableImportModes));
        OnPropertyChanged(nameof(AvailableSyncIntervals));
        OnPropertyChanged(nameof(LanguageHint));
        OnPropertyChanged(nameof(ThemeHint));
        OnPropertyChanged(nameof(DateFormatHint));
        OnPropertyChanged(nameof(ImportModeHint));
        OnPropertyChanged(nameof(SyncIntervalHint));
        OnPropertyChanged(nameof(AutoExportHint));
        OnPropertyChanged(nameof(NotificationsHint));
        OnPropertyChanged(nameof(SyncIndicatorHint));
        OnPropertyChanged(nameof(ExportPathHint));
        OnPropertyChanged(nameof(ImportHint));
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

    private void BuildAvailableLanguages()
    {
        RefreshLocalizedOptions(
            ref _availableLanguages,
            [
                (AppLanguages.Russian, _localization.GetString("Language.Russian")),
                (AppLanguages.English, _localization.GetString("Language.English"))
            ],
            static (code, displayName) => new LanguageOption(code, displayName),
            static (option, displayName) => option.DisplayName = displayName,
            value => AvailableLanguages = value);
    }

    private void BuildAvailableThemes()
    {
        RefreshLocalizedOptions(
            ref _availableThemes,
            _themeService.AvailableThemes
                .Select(theme => (theme, _localization.GetString($"Settings.Theme.{theme}")))
                .ToArray(),
            static (code, displayName) => new ThemeOption(code, displayName),
            static (option, displayName) => option.DisplayName = displayName,
            value => AvailableThemes = value);
    }

    private void BuildAvailableDateFormats()
    {
        RefreshLocalizedOptions(
            ref _availableDateFormats,
            [
                (DateDisplayFormats.DayMonthYear, _localization.GetString("Settings.DateFormat.DayMonthYear")),
                (DateDisplayFormats.MonthDayYear, _localization.GetString("Settings.DateFormat.MonthDayYear")),
                (DateDisplayFormats.YearMonthDay, _localization.GetString("Settings.DateFormat.YearMonthDay"))
            ],
            static (code, displayName) => new DateFormatOption(code, displayName),
            static (option, displayName) => option.DisplayName = displayName,
            value => AvailableDateFormats = value);
    }

    private void BuildAvailableImportModes()
    {
        RefreshLocalizedOptions(
            ref _availableImportModes,
            [
                (BirdImportModes.Merge, _localization.GetString("Settings.ImportMode.Merge")),
                (BirdImportModes.Replace, _localization.GetString("Settings.ImportMode.Replace"))
            ],
            static (code, displayName) => new ImportModeOption(code, displayName),
            static (option, displayName) => option.DisplayName = displayName,
            value => AvailableImportModes = value);
    }

    private void BuildAvailableSyncIntervals()
    {
        RefreshLocalizedOptions(
            ref _availableSyncIntervals,
            [
                (RemoteSyncIntervalPresets.FiveSeconds, _localization.GetString("Settings.SyncIntervalOption.FiveSeconds")),
                (RemoteSyncIntervalPresets.TenSeconds, _localization.GetString("Settings.SyncIntervalOption.TenSeconds")),
                (RemoteSyncIntervalPresets.ThirtySeconds, _localization.GetString("Settings.SyncIntervalOption.ThirtySeconds")),
                (RemoteSyncIntervalPresets.OneMinute, _localization.GetString("Settings.SyncIntervalOption.OneMinute"))
            ],
            static (code, displayName) => new SyncIntervalOption(code, displayName),
            static (option, displayName) => option.DisplayName = displayName,
            value => AvailableSyncIntervals = value);
    }

    private void ReloadFromPreferences(bool reapplyTheme = false)
    {
        var normalizedLanguage = AppLanguages.Normalize(_preferences.SelectedLanguage);
        var normalizedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);
        var normalizedDateFormat = DateDisplayFormats.Normalize(_preferences.SelectedDateFormat);
        var normalizedImportMode = BirdImportModes.Normalize(_preferences.SelectedImportMode);
        var normalizedSyncInterval = RemoteSyncIntervalPresets.Normalize(_preferences.SelectedSyncInterval);

        _isSynchronizingSelections = true;
        try
        {
            SelectedLanguage = normalizedLanguage;
            SelectedTheme = normalizedTheme;
            SelectedDateFormat = normalizedDateFormat;
            SelectedImportMode = normalizedImportMode;
            SelectedSyncInterval = normalizedSyncInterval;
            AutoExportEnabled = _preferences.AutoExportEnabled;
            ShowNotificationBadge = _preferences.ShowNotificationBadge;
            ShowSyncStatusIndicator = _preferences.ShowSyncStatusIndicator;
        }
        finally
        {
            _isSynchronizingSelections = false;
        }

        if (reapplyTheme)
            _themeService.ApplyTheme(normalizedTheme);

        OnPropertyChanged(nameof(LanguageHint));
        OnPropertyChanged(nameof(ThemeHint));
        OnPropertyChanged(nameof(DateFormatHint));
        OnPropertyChanged(nameof(ImportModeHint));
        OnPropertyChanged(nameof(SyncIntervalHint));
        OnPropertyChanged(nameof(AutoExportHint));
        OnPropertyChanged(nameof(NotificationsHint));
        OnPropertyChanged(nameof(SyncIndicatorHint));
        OnPropertyChanged(nameof(ExportPathHint));
        OnPropertyChanged(nameof(ImportHint));
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

    private bool CanTransferData()
    {
        return !IsDataTransferBusy && !IsDangerZoneBusy;
    }

    private bool CanStartDangerAction()
    {
        return !IsDataTransferBusy && !IsDangerZoneBusy;
    }

    private bool CanSyncNow()
    {
        return IsRemoteSyncConfigured
               && !IsDataTransferBusy
               && !IsDangerZoneBusy
               && !IsSyncControlBusy
               && !IsRemoteSyncSyncing;
    }

    private bool CanToggleRemoteSyncPause()
    {
        return IsRemoteSyncConfigured
               && !IsDataTransferBusy
               && !IsDangerZoneBusy
               && !IsSyncControlBusy;
    }

    private bool CanBeginRedownloadRemoteSnapshot()
    {
        return IsRemoteSyncConfigured
               && !IsDataTransferBusy
               && !IsDangerZoneBusy
               && !IsSyncControlBusy;
    }

    private bool CanConfirmRedownloadRemoteSnapshot()
    {
        return IsConfirmingRedownloadRemoteSnapshot && CanBeginRedownloadRemoteSnapshot();
    }

    private bool CanBeginUploadLocalSnapshotToRemote()
    {
        return IsRemoteSyncConfigured
               && !IsDataTransferBusy
               && !IsDangerZoneBusy
               && !IsSyncControlBusy;
    }

    private bool CanConfirmUploadLocalSnapshotToRemote()
    {
        return IsConfirmingUploadLocalSnapshotToRemote && CanBeginUploadLocalSnapshotToRemote();
    }

    private bool CanConfirmClearBirdRecords()
    {
        return IsConfirmingClearBirdRecords && CanStartDangerAction();
    }

    private bool CanConfirmResetLocalDatabase()
    {
        return SupportsLocalDatabaseReset && IsConfirmingResetLocalDatabase && CanStartDangerAction();
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
            OnPropertyChanged(nameof(IsRemoteSyncConfigured));
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

    private string ResolveExportPath()
    {
        return string.IsNullOrWhiteSpace(_preferences.CustomExportPath)
            ? _exportPathProvider.GetLatestPath("birds")
            : _preferences.CustomExportPath;
    }

    private static DateTime ToLocalTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return utc.ToLocalTime();
    }

    private static void RefreshLocalizedOptions<TOption>(
        ref ReadOnlyCollection<TOption> current,
        IReadOnlyList<(string Code, string DisplayName)> entries,
        Func<string, string, TOption> factory,
        Action<TOption, string> updateDisplayName,
        Action<ReadOnlyCollection<TOption>> assign)
        where TOption : class
    {
        if (current.Count == entries.Count)
        {
            for (var index = 0; index < entries.Count; index++)
                updateDisplayName(current[index], entries[index].DisplayName);

            return;
        }

        current = new ReadOnlyCollection<TOption>(
            entries.Select(entry => factory(entry.Code, entry.DisplayName)).ToList());
        assign(current);
    }
}
