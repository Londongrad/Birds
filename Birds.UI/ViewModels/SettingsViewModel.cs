using Birds.Application.Commands.ImportBirds;
using Birds.Shared.Localization;
using Birds.Application.Interfaces;
using Birds.UI.Services.Dialogs.Interfaces;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Import.Interfaces;
using Birds.UI.Services.Import;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using Birds.UI.Services.Sync;
using Birds.Shared.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace Birds.UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IAppPreferencesService _preferences;
        private readonly IThemeService _themeService;
        private readonly ILocalizationService _localization;
        private readonly IBirdManager _birdManager;
        private readonly IExportService _exportService;
        private readonly IExportPathProvider _exportPathProvider;
        private readonly IAutoExportCoordinator _autoExportCoordinator;
        private readonly IImportService _importService;
        private readonly IDataFileDialogService _dataFileDialogService;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;
        private readonly IDatabaseMaintenanceService _databaseMaintenanceService;
        private readonly IRemoteSyncStatusSource _remoteSyncStatus;
        private bool _isSynchronizingSelections;

        private ReadOnlyCollection<LanguageOption> _availableLanguages =
            new(new List<LanguageOption>());

        private ReadOnlyCollection<ThemeOption> _availableThemes =
            new(new List<ThemeOption>());

        private ReadOnlyCollection<DateFormatOption> _availableDateFormats =
            new(new List<DateFormatOption>());

        private ReadOnlyCollection<ImportModeOption> _availableImportModes =
            new(new List<ImportModeOption>());

        public SettingsViewModel(IAppPreferencesService preferences,
                                 IThemeService themeService,
                                 ILocalizationService localization,
                                 IBirdManager birdManager,
                                 IExportService exportService,
                                 IExportPathProvider exportPathProvider,
                                 IAutoExportCoordinator autoExportCoordinator,
                                 IImportService importService,
                                 IDataFileDialogService dataFileDialogService,
                                 INotificationService notificationService,
                                 IMediator mediator,
                                 IDatabaseMaintenanceService databaseMaintenanceService,
                                 IRemoteSyncStatusSource remoteSyncStatus)
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
            _notificationService = notificationService;
            _mediator = mediator;
            _databaseMaintenanceService = databaseMaintenanceService;
            _remoteSyncStatus = remoteSyncStatus;

            BuildAvailableLanguages();
            BuildAvailableThemes();
            BuildAvailableDateFormats();
            BuildAvailableImportModes();
            ReloadFromPreferences();

            _preferences.PropertyChanged += OnPreferencesChanged;
            _localization.LanguageChanged += OnLanguageChanged;
            _remoteSyncStatus.PropertyChanged += OnRemoteSyncStatusChanged;
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

        [ObservableProperty]
        private string selectedLanguage = AppPreferencesState.DefaultLanguage;

        [ObservableProperty]
        private string selectedTheme = AppPreferencesState.DefaultTheme;

        [ObservableProperty]
        private string selectedDateFormat = AppPreferencesState.DefaultDateFormat;

        [ObservableProperty]
        private string selectedImportMode = AppPreferencesState.DefaultImportMode;

        [ObservableProperty]
        private bool autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;

        [ObservableProperty]
        private bool showNotificationBadge = true;

        [ObservableProperty]
        private bool reduceMotion;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportDataCommand))]
        [NotifyCanExecuteChangedFor(nameof(ImportDataCommand))]
        [NotifyCanExecuteChangedFor(nameof(BeginClearBirdRecordsCommand))]
        [NotifyCanExecuteChangedFor(nameof(BeginResetLocalDatabaseCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
        private bool isDataTransferBusy;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportDataCommand))]
        [NotifyCanExecuteChangedFor(nameof(ImportDataCommand))]
        [NotifyCanExecuteChangedFor(nameof(BeginClearBirdRecordsCommand))]
        [NotifyCanExecuteChangedFor(nameof(BeginResetLocalDatabaseCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
        private bool isDangerZoneBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDangerConfirmationVisible))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmClearBirdRecordsCommand))]
        private bool isConfirmingClearBirdRecords;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDangerConfirmationVisible))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmResetLocalDatabaseCommand))]
        private bool isConfirmingResetLocalDatabase;

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

        public string MotionHint =>
            ReduceMotion
                ? _localization.GetString("Settings.MotionHint.Enabled")
                : _localization.GetString("Settings.MotionHint.Disabled");

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

        public RemoteSyncDisplayState RemoteSyncStatus => _remoteSyncStatus.Status;

        public string RemoteSyncStatusLabel => RemoteSyncStatusTextFormatter.GetLabel(_localization, RemoteSyncStatus);

        public string RemoteSyncStatusHint => RemoteSyncStatusTextFormatter.GetHint(_localization, _remoteSyncStatus);

        public bool SupportsLocalDatabaseReset => _databaseMaintenanceService.CanResetLocalDatabase;

        public bool IsDangerConfirmationVisible => IsConfirmingClearBirdRecords || IsConfirmingResetLocalDatabase;

        [RelayCommand]
        private void ResetPreferences()
        {
            _preferences.ResetToDefaults();
            ReloadFromPreferences();
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

        [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
        private void BeginClearBirdRecords()
        {
            IsConfirmingResetLocalDatabase = false;
            IsConfirmingClearBirdRecords = true;
        }

        [RelayCommand(CanExecute = nameof(CanStartDangerAction))]
        private void BeginResetLocalDatabase()
        {
            if (!SupportsLocalDatabaseReset)
                return;

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
                _birdManager.Store.ReplaceBirds(Array.Empty<Birds.Application.DTOs.BirdDTO>());
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
                _birdManager.Store.ReplaceBirds(Array.Empty<Birds.Application.DTOs.BirdDTO>());
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
                {
                    _notificationService.ShowSuccessLocalized(
                        "Info.ImportReplacedSucceeded",
                        importResult.Value.Imported,
                        importResult.Value.Added,
                        importResult.Value.Updated,
                        importResult.Value.Removed);
                }
                else
                {
                    _notificationService.ShowSuccessLocalized(
                        "Info.ImportMergedSucceeded",
                        importResult.Value.Imported,
                        importResult.Value.Added,
                        importResult.Value.Updated);
                }
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

        partial void OnReduceMotionChanged(bool value)
        {
            if (_isSynchronizingSelections)
            {
                OnPropertyChanged(nameof(MotionHint));
                return;
            }

            if (_preferences.ReduceMotion != value)
                _preferences.ReduceMotion = value;

            OnPropertyChanged(nameof(MotionHint));
        }

        private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IAppPreferencesService.SelectedLanguage)
                or nameof(IAppPreferencesService.SelectedTheme)
                or nameof(IAppPreferencesService.SelectedDateFormat)
                or nameof(IAppPreferencesService.SelectedImportMode)
                or nameof(IAppPreferencesService.CustomExportPath)
                or nameof(IAppPreferencesService.AutoExportEnabled)
                or nameof(IAppPreferencesService.ShowNotificationBadge)
                or nameof(IAppPreferencesService.ReduceMotion))
            {
                ReloadFromPreferences(reapplyTheme: true);
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            var preservedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);

            BuildAvailableLanguages();
            BuildAvailableThemes();
            BuildAvailableDateFormats();
            BuildAvailableImportModes();
            ReloadFromPreferences(reapplyTheme: true);
            _themeService.ApplyTheme(preservedTheme);
            OnPropertyChanged(nameof(LanguageHint));
            OnPropertyChanged(nameof(ThemeHint));
            OnPropertyChanged(nameof(DateFormatHint));
            OnPropertyChanged(nameof(ImportModeHint));
            OnPropertyChanged(nameof(AutoExportHint));
            OnPropertyChanged(nameof(NotificationsHint));
            OnPropertyChanged(nameof(MotionHint));
            OnPropertyChanged(nameof(ExportPathHint));
            OnPropertyChanged(nameof(ImportHint));
            OnPropertyChanged(nameof(RemoteSyncStatusLabel));
            OnPropertyChanged(nameof(RemoteSyncStatusHint));
        }

        private void BuildAvailableLanguages()
        {
            AvailableLanguages = new ReadOnlyCollection<LanguageOption>(
                new List<LanguageOption>
                {
                    new(AppLanguages.Russian, _localization.GetString("Language.Russian")),
                    new(AppLanguages.English, _localization.GetString("Language.English"))
                });
        }

        private void BuildAvailableThemes()
        {
            AvailableThemes = new ReadOnlyCollection<ThemeOption>(
                _themeService.AvailableThemes
                    .Select(theme => new ThemeOption(theme, _localization.GetString($"Settings.Theme.{theme}")))
                    .ToList());
        }

        private void BuildAvailableDateFormats()
        {
            AvailableDateFormats = new ReadOnlyCollection<DateFormatOption>(
                new List<DateFormatOption>
                {
                    new(DateDisplayFormats.DayMonthYear, _localization.GetString("Settings.DateFormat.DayMonthYear")),
                    new(DateDisplayFormats.MonthDayYear, _localization.GetString("Settings.DateFormat.MonthDayYear")),
                    new(DateDisplayFormats.YearMonthDay, _localization.GetString("Settings.DateFormat.YearMonthDay"))
                });
        }

        private void BuildAvailableImportModes()
        {
            AvailableImportModes = new ReadOnlyCollection<ImportModeOption>(
                new List<ImportModeOption>
                {
                    new(BirdImportModes.Merge, _localization.GetString("Settings.ImportMode.Merge")),
                    new(BirdImportModes.Replace, _localization.GetString("Settings.ImportMode.Replace"))
                });
        }

        private void ReloadFromPreferences(bool reapplyTheme = false)
        {
            var normalizedLanguage = AppLanguages.Normalize(_preferences.SelectedLanguage);
            var normalizedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);
            var normalizedDateFormat = DateDisplayFormats.Normalize(_preferences.SelectedDateFormat);
            var normalizedImportMode = BirdImportModes.Normalize(_preferences.SelectedImportMode);

            _isSynchronizingSelections = true;
            try
            {
                SelectedLanguage = normalizedLanguage;
                SelectedTheme = normalizedTheme;
                SelectedDateFormat = normalizedDateFormat;
                SelectedImportMode = normalizedImportMode;
                AutoExportEnabled = _preferences.AutoExportEnabled;
                ShowNotificationBadge = _preferences.ShowNotificationBadge;
                ReduceMotion = _preferences.ReduceMotion;
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
            OnPropertyChanged(nameof(AutoExportHint));
            OnPropertyChanged(nameof(NotificationsHint));
            OnPropertyChanged(nameof(MotionHint));
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

        private bool CanTransferData() => !IsDataTransferBusy && !IsDangerZoneBusy;

        private bool CanStartDangerAction() => !IsDataTransferBusy && !IsDangerZoneBusy;

        private bool CanConfirmClearBirdRecords()
            => IsConfirmingClearBirdRecords && CanStartDangerAction();

        private bool CanConfirmResetLocalDatabase()
            => SupportsLocalDatabaseReset && IsConfirmingResetLocalDatabase && CanStartDangerAction();

        private void OnRemoteSyncStatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IRemoteSyncStatusSource.Status)
                or nameof(IRemoteSyncStatusSource.LastSuccessfulSyncAtUtc)
                or nameof(IRemoteSyncStatusSource.LastAttemptAtUtc)
                or nameof(IRemoteSyncStatusSource.LastErrorMessage)
                or nameof(IRemoteSyncStatusSource.LastProcessedCount))
            {
                OnPropertyChanged(nameof(RemoteSyncStatus));
                OnPropertyChanged(nameof(RemoteSyncStatusLabel));
                OnPropertyChanged(nameof(RemoteSyncStatusHint));
            }
        }

        private string ResolveExportPath()
            => string.IsNullOrWhiteSpace(_preferences.CustomExportPath)
                ? _exportPathProvider.GetLatestPath("birds")
                : _preferences.CustomExportPath;
    }
}
