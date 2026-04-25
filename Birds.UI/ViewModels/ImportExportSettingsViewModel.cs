using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Birds.Application.Commands.ImportBirds;
using Birds.Application.DTOs;
using Birds.Shared.Localization;
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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Birds.UI.ViewModels;

public partial class ImportExportSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IAutoExportCoordinator _autoExportCoordinator;
    private readonly IBirdManager _birdManager;
    private readonly IDataFileDialogService _dataFileDialogService;
    private readonly IExportPathProvider _exportPathProvider;
    private readonly IExportService _exportService;
    private readonly IImportService _importService;
    private readonly ILocalizationService _localization;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly IPathNavigationService _pathNavigationService;
    private readonly IAppPreferencesService _preferences;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _disposed;
    private bool _isSynchronizingSelections;
    private CancellationTokenSource? _transferCancellation;

    private ReadOnlyCollection<ImportModeOption> _availableImportModes =
        new(new List<ImportModeOption>());

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChooseExportPathCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenExportFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenExportFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportDataCommand))]
    private bool isExternalBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChooseExportPathCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenExportFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenExportFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportDataCommand))]
    private bool isTransferBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImportModeHint))]
    [NotifyPropertyChangedFor(nameof(ImportHint))]
    private string selectedImportMode = AppPreferencesState.DefaultImportMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoExportHint))]
    private bool autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;

    public ImportExportSettingsViewModel(
        IAppPreferencesService preferences,
        ILocalizationService localization,
        IBirdManager birdManager,
        IExportService exportService,
        IExportPathProvider exportPathProvider,
        IAutoExportCoordinator autoExportCoordinator,
        IImportService importService,
        IDataFileDialogService dataFileDialogService,
        IPathNavigationService pathNavigationService,
        INotificationService notificationService,
        IMediator mediator)
    {
        _preferences = preferences;
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

        BuildAvailableImportModes();
        ReloadFromPreferences();

        _preferences.PropertyChanged += OnPreferencesChanged;
        _localization.LanguageChanged += OnLanguageChanged;
    }

    public ReadOnlyCollection<ImportModeOption> AvailableImportModes
    {
        get => _availableImportModes;
        private set => SetProperty(ref _availableImportModes, value);
    }

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

    public void SetExternalBusy(bool value)
    {
        IsExternalBusy = value;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _transferCancellation?.Cancel();
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _localization.LanguageChanged -= OnLanguageChanged;
        _lifetimeCancellation.Dispose();
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
        var operationCancellation = CreateTransferCancellation(cancellationToken);

        IsTransferBusy = true;
        try
        {
            var operationToken = operationCancellation.Token;
            await _birdManager.FlushPendingOperationsAsync(operationToken);
            var snapshot = _birdManager.Store.Birds.ToList();
            await _exportService.ExportAsync(snapshot, targetPath, operationToken);
            if (_disposed)
                return;

            _notificationService.ShowSuccessLocalized("Info.ExportSucceeded", targetPath);
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // User canceled or application is shutting down.
        }
        catch
        {
            _notificationService.ShowErrorLocalized("Error.ExportFailed");
        }
        finally
        {
            if (!_disposed)
                IsTransferBusy = false;

            ClearTransferCancellation(operationCancellation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanTransferData))]
    private async Task ImportDataAsync(CancellationToken cancellationToken)
    {
        var suggestedPath = ResolveExportPath();
        var sourcePath = _dataFileDialogService.PickImportPath(suggestedPath);

        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        var operationCancellation = CreateTransferCancellation(cancellationToken);
        IsTransferBusy = true;
        try
        {
            var operationToken = operationCancellation.Token;
            var importPayload = await _importService.ImportAsync(sourcePath, operationToken);
            if (!importPayload.IsSuccess)
            {
                if (_disposed)
                    return;

                _notificationService.ShowError(importPayload.Error ?? _localization.GetString("Error.ImportFailed"));
                return;
            }

            await _birdManager.FlushPendingOperationsAsync(operationToken);
            var importMode = BirdImportModes.ToCommandMode(SelectedImportMode);
            var importResult = await _mediator.Send(
                new ImportBirdsCommand(importPayload.Value!, importMode),
                operationToken);
            if (!importResult.IsSuccess)
            {
                if (_disposed)
                    return;

                _notificationService.ShowError(importResult.Error ?? _localization.GetString("Error.ImportFailed"));
                return;
            }

            if (_disposed)
                return;

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
            if (!_disposed)
                IsTransferBusy = false;

            ClearTransferCancellation(operationCancellation);
        }
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

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IAppPreferencesService.SelectedImportMode)
            or nameof(IAppPreferencesService.CustomExportPath)
            or nameof(IAppPreferencesService.AutoExportEnabled))
            ReloadFromPreferences();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        BuildAvailableImportModes();
        ReloadFromPreferences();
        OnPropertyChanged(nameof(AvailableImportModes));
        OnPropertyChanged(nameof(SelectedImportMode));
        OnPropertyChanged(nameof(ImportModeHint));
        OnPropertyChanged(nameof(AutoExportHint));
        OnPropertyChanged(nameof(ExportPathHint));
        OnPropertyChanged(nameof(ImportHint));
    }

    private void BuildAvailableImportModes()
    {
        AvailableImportModes = CreateLocalizedOptions(
            [
                (BirdImportModes.Merge, _localization.GetString("Settings.ImportMode.Merge")),
                (BirdImportModes.Replace, _localization.GetString("Settings.ImportMode.Replace"))
            ],
            static (code, displayName) => new ImportModeOption(code, displayName));
    }

    private void ReloadFromPreferences()
    {
        var normalizedImportMode = BirdImportModes.Normalize(_preferences.SelectedImportMode);

        _isSynchronizingSelections = true;
        try
        {
            SelectedImportMode = normalizedImportMode;
            AutoExportEnabled = _preferences.AutoExportEnabled;
        }
        finally
        {
            _isSynchronizingSelections = false;
        }

        OnPropertyChanged(nameof(ImportModeHint));
        OnPropertyChanged(nameof(AutoExportHint));
        OnPropertyChanged(nameof(ExportPathHint));
        OnPropertyChanged(nameof(ImportHint));
    }

    private bool CanTransferData()
    {
        return !IsTransferBusy && !IsExternalBusy;
    }

    private CancellationTokenSource CreateTransferCancellation(CancellationToken cancellationToken)
    {
        var previous = _transferCancellation;
        var current = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        _transferCancellation = current;

        previous?.Cancel();
        previous?.Dispose();

        return current;
    }

    private void ClearTransferCancellation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_transferCancellation, operationCancellation))
            _transferCancellation = null;

        operationCancellation.Dispose();
    }

    private string ResolveExportPath()
    {
        return string.IsNullOrWhiteSpace(_preferences.CustomExportPath)
            ? _exportPathProvider.GetLatestPath("birds")
            : _preferences.CustomExportPath;
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
