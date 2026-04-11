using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Birds.Application.Commands.ImportBirds;
using Birds.Application.Common.Models;
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
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using MediatR;
using Moq;

namespace Birds.Tests.UI.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<IAutoExportCoordinator> _autoExportCoordinator = new();
    private readonly Mock<IBirdManager> _birdManager = new();
    private readonly Mock<IDatabaseMaintenanceService> _databaseMaintenanceService = new();
    private readonly Mock<IDataFileDialogService> _dataFileDialogService = new();
    private readonly Mock<IExportPathProvider> _exportPathProvider = new();
    private readonly Mock<IExportService> _exportService = new();
    private readonly Mock<IImportService> _importService = new();
    private readonly Mock<ILocalizationService> _localization = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly TestPreferencesService _preferences = new();
    private readonly Mock<IRemoteSyncController> _remoteSyncController = new();
    private readonly TestRemoteSyncStatusSource _remoteSyncStatus = new();
    private readonly BirdStore _store = new();
    private readonly Mock<IThemeService> _themeService = new();
    private CultureInfo _culture = CultureInfo.GetCultureInfo(AppLanguages.English);

    public SettingsViewModelTests()
    {
        _themeService.SetupGet(x => x.AvailableThemes)
            .Returns(new ReadOnlyCollection<string>(ThemeKeys.SupportedThemes.ToList()));

        _localization.SetupGet(x => x.CurrentCulture).Returns(() => _culture);
        _localization.SetupGet(x => x.CurrentLanguage).Returns(() => _culture.Name);
        _localization.SetupGet(x => x.CurrentDateFormat).Returns(DateDisplayFormats.DayMonthYear);
        _localization.Setup(x => x.GetString(It.IsAny<string>()))
            .Returns((string key) => AppText.Get(key, _culture));
        _localization.Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => AppText.Format(_culture, key, args));

        _birdManager.SetupGet(x => x.Store).Returns(_store);
        _birdManager.Setup(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _exportPathProvider.Setup(x => x.GetLatestPath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("C:\\temp\\birds.json");
        _databaseMaintenanceService.SetupGet(x => x.CanResetLocalDatabase).Returns(true);
        _remoteSyncController.SetupGet(x => x.IsConfigured).Returns(true);
        _remoteSyncController.Setup(x => x.SyncNowAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _remoteSyncController.Setup(x => x.RedownloadRemoteSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _remoteSyncController.Setup(x => x.PauseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _remoteSyncController.Setup(x => x.ResumeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void Hints_Should_Use_LocalizationService_Strings()
    {
        _preferences.SelectedLanguage = AppLanguages.English;
        _preferences.SelectedTheme = ThemeKeys.Graphite;
        _preferences.SelectedDateFormat = DateDisplayFormats.DayMonthYear;
        _preferences.SelectedImportMode = BirdImportModes.Merge;
        _preferences.AutoExportEnabled = true;
        _preferences.ShowNotificationBadge = true;
        _preferences.ShowSyncStatusIndicator = true;

        var sut = CreateSut();

        sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Graphite", _culture));
        sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.DayMonthYear", _culture));
        sut.ImportModeHint.Should().Be(AppText.Get("Settings.ImportModeHint.Merge", _culture));
        sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Enabled", _culture));
        sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Enabled", _culture));
        sut.SyncIndicatorHint.Should().Be(AppText.Get("Settings.SyncIndicatorHint.Enabled", _culture));
    }

    [Fact]
    public void LanguageChanged_Should_Rebuild_Localized_Options_And_Hints()
    {
        _preferences.SelectedLanguage = AppLanguages.Russian;
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.SelectedDateFormat = DateDisplayFormats.YearMonthDay;
        _preferences.SelectedImportMode = BirdImportModes.Replace;
        _preferences.AutoExportEnabled = false;
        _preferences.ShowNotificationBadge = false;
        _preferences.ShowSyncStatusIndicator = false;

        var sut = CreateSut();

        _culture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.AvailableLanguages[0].DisplayName.Should().Be(AppText.Get("Language.Russian", _culture));
        sut.AvailableThemes.Should().Contain(x => x.DisplayName == AppText.Get("Settings.Theme.Steel", _culture));
        sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Steel", _culture));
        sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.YearMonthDay", _culture));
        sut.ImportModeHint.Should().Be(AppText.Get("Settings.ImportModeHint.Replace", _culture));
        sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Disabled", _culture));
        sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Disabled", _culture));
        sut.SyncIndicatorHint.Should().Be(AppText.Get("Settings.SyncIndicatorHint.Disabled", _culture));
    }

    [Fact]
    public void AutoExportEnabledChanged_Should_PersistPreference_And_UpdateHint()
    {
        _preferences.AutoExportEnabled = true;

        var sut = CreateSut();

        sut.AutoExportEnabled = false;

        _preferences.AutoExportEnabled.Should().BeFalse();
        sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Disabled", _culture));
    }

    [Fact]
    public void ShowSyncStatusIndicatorChanged_Should_PersistPreference_And_UpdateHint()
    {
        _preferences.ShowSyncStatusIndicator = true;

        var sut = CreateSut();

        sut.ShowSyncStatusIndicator = false;

        _preferences.ShowSyncStatusIndicator.Should().BeFalse();
        sut.SyncIndicatorHint.Should().Be(AppText.Get("Settings.SyncIndicatorHint.Disabled", _culture));
    }

    [Fact]
    public void RemoteSyncStatus_Should_Project_Localized_Label_And_Hint()
    {
        _remoteSyncStatus.SetState(
            RemoteSyncDisplayState.Offline,
            new DateTime(2026, 4, 8, 10, 15, 0, DateTimeKind.Utc),
            "backend unavailable",
            0,
            3);

        var sut = CreateSut();

        sut.RemoteSyncStatus.Should().Be(RemoteSyncDisplayState.Offline);
        sut.RemoteSyncStatusLabel.Should().Be(AppText.Get("Settings.SyncStatus.Offline", _culture));
        sut.RemoteSyncStatusHint.Should().Contain(AppText.Get("Settings.SyncStatusHint.Offline", _culture));
        sut.RemoteSyncStatusHint.Should().Contain("backend unavailable");
        sut.RemoteSyncPendingCountValue.Should().Be("3");
    }

    [Fact]
    public void RemoteSyncRecentActivity_Should_Project_Localized_Items()
    {
        _remoteSyncStatus.SetState(
            RemoteSyncDisplayState.Synced,
            new DateTime(2026, 4, 8, 10, 15, 0, DateTimeKind.Utc),
            lastProcessedCount: 5,
            pendingOperationCount: 1,
            recentActivity:
            [
                new RemoteSyncActivityEntry(
                    RemoteSyncDisplayState.Synced,
                    new DateTime(2026, 4, 8, 10, 15, 0, DateTimeKind.Utc),
                    5,
                    1)
            ]);

        var sut = CreateSut();

        sut.HasRemoteSyncRecentActivity.Should().BeTrue();
        sut.RemoteSyncRecentActivityLabel.Should().Be(AppText.Get("Settings.SyncMeta.RecentActivityLabel", _culture));
        sut.RemoteSyncRecentActivityItems.Should().ContainSingle();
        sut.RemoteSyncRecentActivityItems[0].Title.Should().Be(AppText.Get("Settings.SyncStatus.Synced", _culture));
        sut.RemoteSyncRecentActivityItems[0].Description.Should()
            .Be(AppText.Format(_culture, "Settings.SyncRecent.SyncedProcessed", 5));
    }

    [Fact]
    public void RemoteSyncRecentActivity_Should_BeCollapsed_ByDefault_And_Toggle()
    {
        var sut = CreateSut();

        sut.IsRemoteSyncRecentActivityExpanded.Should().BeFalse();
        sut.RemoteSyncRecentActivityToggleLabel.Should()
            .Be(AppText.Get("Settings.SyncMeta.RecentActivityExpand", _culture));

        sut.ToggleRemoteSyncRecentActivityCommand.Execute(null);

        sut.IsRemoteSyncRecentActivityExpanded.Should().BeTrue();
        sut.RemoteSyncRecentActivityToggleLabel.Should()
            .Be(AppText.Get("Settings.SyncMeta.RecentActivityCollapse", _culture));
    }

    [Fact]
    public async Task SyncNowCommand_Should_Invoke_RemoteSyncController()
    {
        var sut = CreateSut();

        await sut.SyncNowCommand.ExecuteAsync(null);

        _remoteSyncController.Verify(x => x.SyncNowAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleRemoteSyncPauseCommand_Should_Pause_When_CurrentlyActive()
    {
        var sut = CreateSut();

        await sut.ToggleRemoteSyncPauseCommand.ExecuteAsync(null);

        _remoteSyncController.Verify(x => x.PauseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleRemoteSyncPauseCommand_Should_Resume_When_CurrentlyPaused()
    {
        _remoteSyncStatus.SetState(RemoteSyncDisplayState.Paused, pendingOperationCount: 2);
        var sut = CreateSut();

        await sut.ToggleRemoteSyncPauseCommand.ExecuteAsync(null);

        _remoteSyncController.Verify(x => x.ResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmRedownloadRemoteSnapshotCommand_Should_ResetLocalSnapshot_FromRemote()
    {
        _store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });

        var sut = CreateSut();
        sut.BeginRedownloadRemoteSnapshotCommand.Execute(null);

        await sut.ConfirmRedownloadRemoteSnapshotCommand.ExecuteAsync(null);

        sut.IsConfirmingRedownloadRemoteSnapshot.Should().BeFalse();
        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _remoteSyncController.Verify(x => x.RedownloadRemoteSnapshotAsync(It.IsAny<CancellationToken>()), Times.Once);
        _birdManager.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _autoExportCoordinator.Verify(x => x.MarkDirty(), Times.Once);
        _notificationService.Verify(
            x => x.ShowSuccessLocalized("Info.RemoteSnapshotRedownloaded", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmRedownloadRemoteSnapshotCommand_Should_ShowError_WhenRemoteRestoreFails()
    {
        _remoteSyncController.Setup(x => x.RedownloadRemoteSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        sut.BeginRedownloadRemoteSnapshotCommand.Execute(null);

        await sut.ConfirmRedownloadRemoteSnapshotCommand.ExecuteAsync(null);

        _birdManager.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
        _autoExportCoordinator.Verify(x => x.MarkDirty(), Times.Never);
        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.CannotRedownloadRemoteSnapshot", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void LanguageChanged_Should_Preserve_SelectedTheme_And_Reapply_It()
    {
        _preferences.SelectedLanguage = AppLanguages.Russian;
        _preferences.SelectedTheme = ThemeKeys.Steel;

        var sut = CreateSut();

        _culture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
        _preferences.SelectedTheme.Should().Be(ThemeKeys.Steel);
        _themeService.Verify(x => x.ApplyTheme(ThemeKeys.Steel), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExportDataCommand_Should_ExportCurrentSnapshot_And_ShowSuccess()
    {
        _store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", "desc", new DateOnly(2026, 4, 1), null, true, null, null)
        });
        _preferences.CustomExportPath = "C:\\temp\\birds-export.json";

        var sut = CreateSut();

        await sut.ExportDataCommand.ExecuteAsync(null);

        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _exportService.Verify(
            x => x.ExportAsync(
                It.Is<IEnumerable<BirdDTO>>(items => items.Single().Name == "Sparrow"),
                "C:\\temp\\birds-export.json",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationService.Verify(
            x => x.ShowSuccessLocalized("Info.ExportSucceeded",
                It.Is<object[]>(args => args.Single().Equals("C:\\temp\\birds-export.json"))),
            Times.Once);
    }

    [Fact]
    public void ChooseExportPathCommand_Should_Persist_Selected_Path()
    {
        _dataFileDialogService.Setup(x => x.PickExportPath(It.IsAny<string>()))
            .Returns("C:\\exports\\selected-birds.json");

        var sut = CreateSut();

        sut.ChooseExportPathCommand.Execute(null);

        _preferences.CustomExportPath.Should().Be("C:\\exports\\selected-birds.json");
        sut.ExportPathHint.Should().Contain("selected-birds.json");
    }

    [Fact]
    public async Task ImportDataCommand_Should_ApplyImportedSnapshot_And_ShowSuccess()
    {
        var importedBird =
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null);

        _dataFileDialogService.Setup(x => x.PickImportPath(It.IsAny<string>()))
            .Returns("C:\\temp\\birds-import.json");
        _importService.Setup(x => x.ImportAsync("C:\\temp\\birds-import.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(new[] { importedBird }));
        _mediator.Setup(x => x.Send(It.IsAny<ImportBirdsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BirdImportResultDTO>.Success(
                new BirdImportResultDTO(1, 1, 0, 0, new[] { importedBird })));

        var sut = CreateSut();

        await sut.ImportDataCommand.ExecuteAsync(null);

        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _store.Birds.Should().ContainSingle(x => x.Id == importedBird.Id);
        _autoExportCoordinator.Verify(x => x.MarkDirty(), Times.Once);
        _notificationService.Verify(
            x => x.ShowSuccessLocalized(
                "Info.ImportMergedSucceeded",
                It.Is<object[]>(args =>
                    args.Length == 3 && (int)args[0] == 1 && (int)args[1] == 1 && (int)args[2] == 0)),
            Times.Once);
    }

    [Fact]
    public async Task ImportDataCommand_Should_Use_Replace_Mode_When_Selected()
    {
        var importedBird =
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null);

        _preferences.SelectedImportMode = BirdImportModes.Replace;
        _dataFileDialogService.Setup(x => x.PickImportPath(It.IsAny<string>()))
            .Returns("C:\\temp\\birds-import.json");
        _importService.Setup(x => x.ImportAsync("C:\\temp\\birds-import.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(new[] { importedBird }));
        _mediator.Setup(x => x.Send(It.IsAny<ImportBirdsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BirdImportResultDTO>.Success(
                new BirdImportResultDTO(1, 1, 0, 4, new[] { importedBird })));

        var sut = CreateSut();

        await sut.ImportDataCommand.ExecuteAsync(null);

        _mediator.Verify(
            x => x.Send(
                It.Is<ImportBirdsCommand>(command => command.Mode == BirdImportMode.Replace),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _notificationService.Verify(
            x => x.ShowSuccessLocalized(
                "Info.ImportReplacedSucceeded",
                It.Is<object[]>(args =>
                    args.Length == 4
                    && (int)args[0] == 1
                    && (int)args[1] == 1
                    && (int)args[2] == 0
                    && (int)args[3] == 4)),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmClearBirdRecordsCommand_Should_ClearStore_And_ShowSuccess()
    {
        _store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });
        _databaseMaintenanceService.Setup(x => x.ClearBirdRecordsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();
        sut.BeginClearBirdRecordsCommand.Execute(null);

        await sut.ConfirmClearBirdRecordsCommand.ExecuteAsync(null);

        sut.IsConfirmingClearBirdRecords.Should().BeFalse();
        _store.Birds.Should().BeEmpty();
        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _autoExportCoordinator.Verify(x => x.MarkDirty(), Times.Once);
        _notificationService.Verify(
            x => x.ShowSuccessLocalized("Info.BirdRecordsCleared",
                It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmResetLocalDatabaseCommand_Should_ResetStore_And_ShowSuccess()
    {
        _store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });

        var sut = CreateSut();
        sut.BeginResetLocalDatabaseCommand.Execute(null);

        await sut.ConfirmResetLocalDatabaseCommand.ExecuteAsync(null);

        sut.IsConfirmingResetLocalDatabase.Should().BeFalse();
        _store.Birds.Should().BeEmpty();
        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _databaseMaintenanceService.Verify(x => x.ResetLocalDatabaseAsync(It.IsAny<CancellationToken>()), Times.Once);
        _autoExportCoordinator.Verify(x => x.MarkDirty(), Times.Once);
        _notificationService.Verify(x => x.ShowSuccessLocalized("Info.LocalDatabaseReset", It.IsAny<object[]>()),
            Times.Once);
    }

    private SettingsViewModel CreateSut()
    {
        return new SettingsViewModel(
            _preferences,
            _themeService.Object,
            _localization.Object,
            _birdManager.Object,
            _exportService.Object,
            _exportPathProvider.Object,
            _autoExportCoordinator.Object,
            _importService.Object,
            _dataFileDialogService.Object,
            _notificationService.Object,
            _mediator.Object,
            _databaseMaintenanceService.Object,
            _remoteSyncStatus,
            _remoteSyncController.Object);
    }

    private sealed class TestPreferencesService : IAppPreferencesService
    {
        private bool _autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
        private string _customExportPath = string.Empty;
        private string _selectedDateFormat = DateDisplayFormats.DayMonthYear;
        private string _selectedImportMode = BirdImportModes.Merge;
        private string _selectedLanguage = AppLanguages.Russian;
        private string _selectedTheme = ThemeKeys.Graphite;
        private bool _showNotificationBadge = true;
        private bool _showSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value)
                    return;
                _selectedLanguage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLanguage)));
            }
        }

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme == value)
                    return;
                _selectedTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTheme)));
            }
        }

        public string SelectedDateFormat
        {
            get => _selectedDateFormat;
            set
            {
                if (_selectedDateFormat == value)
                    return;
                _selectedDateFormat = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDateFormat)));
            }
        }

        public bool ShowNotificationBadge
        {
            get => _showNotificationBadge;
            set
            {
                if (_showNotificationBadge == value)
                    return;
                _showNotificationBadge = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNotificationBadge)));
            }
        }

        public bool AutoExportEnabled
        {
            get => _autoExportEnabled;
            set
            {
                if (_autoExportEnabled == value)
                    return;
                _autoExportEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoExportEnabled)));
            }
        }

        public bool ShowSyncStatusIndicator
        {
            get => _showSyncStatusIndicator;
            set
            {
                if (_showSyncStatusIndicator == value)
                    return;
                _showSyncStatusIndicator = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowSyncStatusIndicator)));
            }
        }

        public string CustomExportPath
        {
            get => _customExportPath;
            set
            {
                if (_customExportPath == value)
                    return;
                _customExportPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomExportPath)));
            }
        }

        public string SelectedImportMode
        {
            get => _selectedImportMode;
            set
            {
                if (_selectedImportMode == value)
                    return;
                _selectedImportMode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImportMode)));
            }
        }

        public void ResetToDefaults()
        {
            SelectedLanguage = AppPreferencesState.DefaultLanguage;
            SelectedTheme = AppPreferencesState.DefaultTheme;
            SelectedDateFormat = AppPreferencesState.DefaultDateFormat;
            SelectedImportMode = AppPreferencesState.DefaultImportMode;
            CustomExportPath = string.Empty;
            AutoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
            ShowNotificationBadge = true;
            ShowSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;
        }
    }

    private sealed class TestRemoteSyncStatusSource : IRemoteSyncStatusSource
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public RemoteSyncDisplayState Status { get; private set; } = RemoteSyncDisplayState.Disabled;

        public DateTime? LastSuccessfulSyncAtUtc { get; private set; }

        public DateTime? LastAttemptAtUtc { get; private set; }

        public string? LastErrorMessage { get; private set; }

        public int LastProcessedCount { get; private set; }

        public int PendingOperationCount { get; private set; }

        public IReadOnlyList<RemoteSyncActivityEntry> RecentActivity { get; private set; } =
            Array.Empty<RemoteSyncActivityEntry>();

        public void SetState(RemoteSyncDisplayState status,
            DateTime? lastSuccessfulSyncAtUtc = null,
            string? lastErrorMessage = null,
            int lastProcessedCount = 0,
            int pendingOperationCount = 0,
            IReadOnlyList<RemoteSyncActivityEntry>? recentActivity = null)
        {
            Status = status;
            LastSuccessfulSyncAtUtc = lastSuccessfulSyncAtUtc;
            LastAttemptAtUtc = DateTime.UtcNow;
            LastErrorMessage = lastErrorMessage;
            LastProcessedCount = lastProcessedCount;
            PendingOperationCount = pendingOperationCount;
            RecentActivity = recentActivity ?? Array.Empty<RemoteSyncActivityEntry>();
            RaiseAll();
        }

        private void RaiseAll()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastSuccessfulSyncAtUtc)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastAttemptAtUtc)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastErrorMessage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastProcessedCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingOperationCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentActivity)));
        }
    }
}
