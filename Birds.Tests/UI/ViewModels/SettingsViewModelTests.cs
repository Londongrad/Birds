using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Birds.Application.Commands.ImportBirds;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Tests.Helpers;
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
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Services.Sync;
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
    private readonly Mock<IPathNavigationService> _pathNavigationService = new();
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
        _remoteSyncController.SetupGet(x => x.IsEnabled).Returns(true);
        _remoteSyncController.SetupGet(x => x.IsConfigured).Returns(true);
        _remoteSyncController.SetupGet(x => x.ConfigurationErrorMessage).Returns((string?)null);
        _remoteSyncController.Setup(x => x.SyncNowAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _remoteSyncController.Setup(x => x.UploadLocalSnapshotToRemoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _remoteSyncController.Setup(x => x.RedownloadRemoteSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _remoteSyncController.Setup(x => x.PauseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _remoteSyncController.Setup(x => x.ResumeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _preferences.SelectedSyncInterval = RemoteSyncIntervalPresets.Default;
    }

    [Fact]
    public void AppearanceHints_Should_Use_LocalizationService_Strings()
    {
        _preferences.SelectedLanguage = AppLanguages.English;
        _preferences.SelectedTheme = ThemeKeys.Graphite;
        _preferences.SelectedDateFormat = DateDisplayFormats.DayMonthYear;
        _preferences.SelectedImportMode = BirdImportModes.Merge;
        _preferences.AutoExportEnabled = true;
        _preferences.ShowNotificationBadge = true;
        _preferences.ShowSyncStatusIndicator = true;

        var sut = CreateAppearanceSut();

        sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Graphite", _culture));
        sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.DayMonthYear", _culture));
        sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Enabled", _culture));
        sut.SyncIndicatorHint.Should().Be(AppText.Get("Settings.SyncIndicatorHint.Enabled", _culture));
    }

    [Fact]
    public void ImportExportHints_Should_Use_LocalizationService_Strings()
    {
        _preferences.SelectedImportMode = BirdImportModes.Merge;
        _preferences.AutoExportEnabled = true;

        var sut = CreateImportExportSut();

        sut.ImportModeHint.Should().Be(AppText.Get("Settings.ImportModeHint.Merge", _culture));
        sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Enabled", _culture));
    }

    [Fact]
    public void AppearanceSettings_LanguageChanged_Should_Rebuild_Localized_Options_And_Hints()
    {
        _preferences.SelectedLanguage = AppLanguages.Russian;
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.SelectedDateFormat = DateDisplayFormats.YearMonthDay;
        _preferences.SelectedImportMode = BirdImportModes.Replace;
        _preferences.AutoExportEnabled = false;
        _preferences.ShowNotificationBadge = false;
        _preferences.ShowSyncStatusIndicator = false;

        var sut = CreateAppearanceSut();
        var availableLanguages = sut.AvailableLanguages;
        var availableThemes = sut.AvailableThemes;
        var availableDateFormats = sut.AvailableDateFormats;

        _culture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.AvailableLanguages.Should().NotBeSameAs(availableLanguages);
        sut.AvailableThemes.Should().NotBeSameAs(availableThemes);
        sut.AvailableDateFormats.Should().NotBeSameAs(availableDateFormats);
        sut.AvailableLanguages[0].DisplayName.Should().Be(AppText.Get("Language.Russian", _culture));
        sut.AvailableThemes.Should().Contain(x => x.DisplayName == AppText.Get("Settings.Theme.Steel", _culture));
        sut.AvailableThemes.Single(x => x.Code == ThemeKeys.Steel).DisplayName
            .Should().Be(AppText.Get("Settings.Theme.Steel", _culture));
        sut.AvailableDateFormats.Single(x => x.Code == DateDisplayFormats.YearMonthDay).DisplayName
            .Should().Be(AppText.Get("Settings.DateFormat.YearMonthDay", _culture));
        sut.SelectedLanguageOption.Should().NotBeNull();
        sut.SelectedThemeOption.Should().NotBeNull();
        sut.SelectedDateFormatOption.Should().NotBeNull();
        sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Steel", _culture));
        sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.YearMonthDay", _culture));
        sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Disabled", _culture));
        sut.SyncIndicatorHint.Should().Be(AppText.Get("Settings.SyncIndicatorHint.Disabled", _culture));
    }

    [Fact]
    public void AutoExportEnabledChanged_Should_PersistPreference_And_UpdateHint()
    {
        _preferences.AutoExportEnabled = true;

        var sut = CreateImportExportSut();

        sut.AutoExportEnabled = false;

        _preferences.AutoExportEnabled.Should().BeFalse();
        sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Disabled", _culture));
    }

    [Fact]
    public void ImportExportSettings_Should_Ignore_Transient_Null_ImportMode_Selection()
    {
        _preferences.SelectedImportMode = BirdImportModes.Replace;
        var sut = CreateImportExportSut();

        sut.SelectedImportMode = null!;
        sut.SelectedImportModeOption = null!;

        sut.SelectedImportMode.Should().Be(BirdImportModes.Replace);
        sut.SelectedImportModeOption.Should().NotBeNull();
        _preferences.SelectedImportMode.Should().Be(BirdImportModes.Replace);
        sut.ImportModeHint.Should().Be(AppText.Get("Settings.ImportModeHint.Replace", _culture));
    }

    [Fact]
    public void ImportExport_LanguageChanged_Should_Update_Localized_Text()
    {
        _preferences.SelectedImportMode = BirdImportModes.Replace;
        _preferences.AutoExportEnabled = false;
        var sut = CreateImportExportSut();
        var availableImportModes = sut.AvailableImportModes;
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                changedProperties.Add(args.PropertyName!);
        };

        _culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.AvailableImportModes.Should().NotBeSameAs(availableImportModes);
        sut.AvailableImportModes.Single(x => x.Code == BirdImportModes.Replace).DisplayName
            .Should().Be(AppText.Get("Settings.ImportMode.Replace", _culture));
        sut.SelectedImportModeOption.Should().NotBeNull();
        sut.SelectedImportModeOption!.DisplayName.Should().Be(AppText.Get("Settings.ImportMode.Replace", _culture));
        sut.ImportModeHint.Should().Be(AppText.Get("Settings.ImportModeHint.Replace", _culture));
        sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Disabled", _culture));
        changedProperties.Should().Contain(nameof(ImportExportSettingsViewModel.AvailableImportModes));
        changedProperties.Should().Contain(nameof(ImportExportSettingsViewModel.SelectedImportMode));
        changedProperties.Should().Contain(nameof(ImportExportSettingsViewModel.SelectedImportModeOption));
        changedProperties.Should().Contain(nameof(ImportExportSettingsViewModel.ImportModeHint));
        changedProperties.Should().Contain(nameof(ImportExportSettingsViewModel.AutoExportHint));
    }

    [Fact]
    public void ImportExport_Dispose_Should_Unsubscribe_From_LongLivedEvents()
    {
        var sut = CreateImportExportSut();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                changedProperties.Add(args.PropertyName!);
        };

        sut.Dispose();
        _preferences.AutoExportEnabled = false;
        _preferences.SelectedImportMode = BirdImportModes.Replace;
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        changedProperties.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportExport_Dispose_Should_Cancel_Running_Export()
    {
        _store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", "desc", new DateOnly(2026, 4, 1), null, true, null, null)
        });
        var exportStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _exportService.Setup(x => x.ExportAsync(
                It.IsAny<IEnumerable<BirdDTO>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<BirdDTO>, string, CancellationToken>(async (_, _, token) =>
            {
                exportStarted.TrySetResult(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            });

        var sut = CreateImportExportSut();
        var exportTask = sut.ExportDataCommand.ExecuteAsync(null);
        var exportToken = await exportStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        sut.Dispose();

        exportToken.IsCancellationRequested.Should().BeTrue();
        await exportTask.WaitAsync(TimeSpan.FromSeconds(3));
        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.ExportFailed", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public void SelectedLanguageChanged_Should_PersistPreference_ApplyLocalization_And_ReloadBirds()
    {
        _preferences.SelectedLanguage = AppLanguages.Russian;
        _localization.Setup(x => x.ApplyLanguage(AppLanguages.English)).Returns(true);

        var sut = CreateAppearanceSut();

        sut.SelectedLanguage = AppLanguages.English;

        _preferences.SelectedLanguage.Should().Be(AppLanguages.English);
        _localization.Verify(x => x.ApplyLanguage(AppLanguages.English), Times.Once);
        _birdManager.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SelectedLanguageChanged_Should_Cancel_Previous_Reload_When_Superseded()
    {
        _preferences.SelectedLanguage = AppLanguages.Russian;
        _localization.Setup(x => x.ApplyLanguage(It.IsAny<string>())).Returns(true);
        var firstReloadStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReloadStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var reloadCallCount = 0;
        _birdManager.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async token =>
            {
                var callNumber = Interlocked.Increment(ref reloadCallCount);
                if (callNumber == 1)
                    firstReloadStarted.TrySetResult(token);
                else
                    secondReloadStarted.TrySetResult(token);

                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            });

        var sut = CreateAppearanceSut();
        sut.SelectedLanguage = AppLanguages.English;
        var firstToken = await firstReloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        sut.SelectedLanguage = AppLanguages.Russian;
        var secondToken = await secondReloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        firstToken.IsCancellationRequested.Should().BeTrue();
        secondToken.CanBeCanceled.Should().BeTrue();
        sut.Dispose();
        secondToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void SelectedThemeChanged_Should_PersistPreference_And_ApplyTheme()
    {
        _preferences.SelectedTheme = ThemeKeys.Graphite;

        var sut = CreateAppearanceSut();

        sut.SelectedTheme = ThemeKeys.Steel;

        _preferences.SelectedTheme.Should().Be(ThemeKeys.Steel);
        _themeService.Verify(x => x.ApplyTheme(ThemeKeys.Steel), Times.AtLeastOnce);
    }

    [Fact]
    public void SelectedDateFormatChanged_Should_PersistPreference_And_ApplyDateFormat()
    {
        _preferences.SelectedDateFormat = DateDisplayFormats.DayMonthYear;

        var sut = CreateAppearanceSut();

        sut.SelectedDateFormat = DateDisplayFormats.YearMonthDay;

        _preferences.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
        _localization.Verify(x => x.ApplyDateFormat(DateDisplayFormats.YearMonthDay), Times.Once);
    }

    [Fact]
    public void AppearanceSettings_Should_Ignore_Transient_Null_ComboBox_Selections()
    {
        _preferences.SelectedLanguage = AppLanguages.English;
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.SelectedDateFormat = DateDisplayFormats.YearMonthDay;
        var sut = CreateAppearanceSut();
        _localization.Invocations.Clear();
        _themeService.Invocations.Clear();
        _birdManager.Invocations.Clear();

        sut.SelectedLanguage = null!;
        sut.SelectedTheme = null!;
        sut.SelectedDateFormat = null!;
        sut.SelectedLanguageOption = null!;
        sut.SelectedThemeOption = null!;
        sut.SelectedDateFormatOption = null!;

        sut.SelectedLanguage.Should().Be(AppLanguages.English);
        sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
        sut.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
        sut.SelectedLanguageOption.Should().NotBeNull();
        sut.SelectedThemeOption.Should().NotBeNull();
        sut.SelectedDateFormatOption.Should().NotBeNull();
        _preferences.SelectedLanguage.Should().Be(AppLanguages.English);
        _preferences.SelectedTheme.Should().Be(ThemeKeys.Steel);
        _preferences.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
        _localization.Verify(x => x.ApplyLanguage(It.IsAny<string>()), Times.Never);
        _localization.Verify(x => x.ApplyDateFormat(It.IsAny<string>()), Times.Never);
        _themeService.Verify(x => x.ApplyTheme(It.IsAny<string>()), Times.Never);
        _birdManager.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void AppearanceSettings_Dispose_Should_Unsubscribe_From_LongLivedEvents()
    {
        var sut = CreateAppearanceSut();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                changedProperties.Add(args.PropertyName!);
        };

        sut.Dispose();
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.SelectedDateFormat = DateDisplayFormats.YearMonthDay;
        _preferences.ShowNotificationBadge = false;
        _preferences.ShowSyncStatusIndicator = false;
        _culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        changedProperties.Should().BeEmpty();
    }

    [Fact]
    public void ShowSyncStatusIndicatorChanged_Should_PersistPreference_And_UpdateHint()
    {
        _preferences.ShowSyncStatusIndicator = true;

        var sut = CreateAppearanceSut();

        sut.ShowSyncStatusIndicator = false;

        _preferences.ShowSyncStatusIndicator.Should().BeFalse();
        sut.SyncIndicatorHint.Should().Be(AppText.Get("Settings.SyncIndicatorHint.Disabled", _culture));
    }

    [Fact]
    public void SyncIntervalChanged_Should_PersistPreference_And_UpdateHint()
    {
        _preferences.SelectedSyncInterval = RemoteSyncIntervalPresets.TenSeconds;

        var sut = CreateSyncSut();

        sut.SelectedSyncInterval = RemoteSyncIntervalPresets.ThirtySeconds;

        _preferences.SelectedSyncInterval.Should().Be(RemoteSyncIntervalPresets.ThirtySeconds);
        sut.SyncIntervalHint.Should().Contain(AppText.Get("Settings.SyncIntervalOption.ThirtySeconds", _culture));
    }

    [Fact]
    public void SyncIntervalChanged_Should_Work_WhenRemoteSyncIsDisabled()
    {
        _remoteSyncController.SetupGet(x => x.IsEnabled).Returns(false);
        _remoteSyncController.SetupGet(x => x.IsConfigured).Returns(false);
        _preferences.SelectedSyncInterval = RemoteSyncIntervalPresets.OneMinute;

        var sut = CreateSyncSut();

        sut.SelectedSyncInterval = RemoteSyncIntervalPresets.FiveSeconds;

        _preferences.SelectedSyncInterval.Should().Be(RemoteSyncIntervalPresets.FiveSeconds);
        sut.SyncIntervalHint.Should().Contain(AppText.Get("Settings.SyncIntervalOption.FiveSeconds", _culture));
    }

    [Fact]
    public void SyncSettings_Should_Ignore_Transient_Null_SyncInterval_Selection()
    {
        _preferences.SelectedSyncInterval = RemoteSyncIntervalPresets.OneMinute;
        var sut = CreateSyncSut();

        sut.SelectedSyncInterval = null!;
        sut.SelectedSyncIntervalOption = null!;

        sut.SelectedSyncInterval.Should().Be(RemoteSyncIntervalPresets.OneMinute);
        sut.SelectedSyncIntervalOption.Should().NotBeNull();
        _preferences.SelectedSyncInterval.Should().Be(RemoteSyncIntervalPresets.OneMinute);
        sut.SyncIntervalHint.Should().Contain(AppText.Get("Settings.SyncIntervalOption.OneMinute", _culture));
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

        var sut = CreateSyncSut();

        sut.RemoteSyncStatus.Should().Be(RemoteSyncDisplayState.Offline);
        sut.RemoteSyncStatusLabel.Should().Be(AppText.Get("Settings.SyncStatus.Offline", _culture));
        sut.RemoteSyncStatusHint.Should().Contain(AppText.Get("Settings.SyncStatusHint.Offline", _culture));
        sut.RemoteSyncStatusHint.Should().Contain("backend unavailable");
        sut.RemoteSyncPendingCountValue.Should().Be("3");
    }

    [Fact]
    public void RemoteSyncStatus_Should_ShowWarning_WhenRemoteIsEmpty_AndLocalArchiveHasData()
    {
        _store.ReplaceBirds([
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        ]);
        _remoteSyncStatus.SetState(
            RemoteSyncDisplayState.Synced,
            new DateTime(2026, 4, 8, 10, 15, 0, DateTimeKind.Utc),
            remoteSnapshotState: RemoteSyncSnapshotState.Empty,
            remoteBirdCount: 0);

        var sut = CreateSyncSut();

        sut.RemoteSnapshotStateValue.Should().Be(AppText.Get("Settings.SyncMeta.RemoteStateEmpty", _culture));
        sut.IsRemoteSnapshotEmptyWarningVisible.Should().BeTrue();
        sut.RemoteSyncStatusHint.Should().Contain(AppText.Get("Settings.SyncStatusHint.RemoteEmpty", _culture));
    }

    [Fact]
    public void RemoteSnapshotState_Should_ShowLoading_WhenSyncingAndRemoteStateIsUnknown()
    {
        _remoteSyncStatus.SetState(
            RemoteSyncDisplayState.Syncing,
            remoteSnapshotState: RemoteSyncSnapshotState.Unknown);

        var sut = CreateSyncSut();

        sut.IsRemoteSnapshotStateLoading.Should().BeTrue();
        sut.RemoteSnapshotStateValue.Should().Be(AppText.Get("Settings.SyncMeta.RemoteStateLoading", _culture));
    }

    [Fact]
    public void RemoteSnapshotState_Should_KeepKnownCount_WhenSyncingAndRemoteStateIsKnown()
    {
        _remoteSyncStatus.SetState(
            RemoteSyncDisplayState.Syncing,
            remoteSnapshotState: RemoteSyncSnapshotState.HasData,
            remoteBirdCount: 12);

        var sut = CreateSyncSut();

        sut.IsRemoteSnapshotStateLoading.Should().BeFalse();
        sut.RemoteSnapshotStateValue.Should().Be(AppText.Format(_culture, "Settings.SyncMeta.RemoteStateHasDataCount", 12));
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

        var sut = CreateSyncSut();

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
        var sut = CreateSyncSut();

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
        CancellationToken capturedToken = default;
        _remoteSyncController.Setup(x => x.SyncNowAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedToken = token)
            .Returns(Task.CompletedTask);
        var sut = CreateSyncSut();

        await sut.SyncNowCommand.ExecuteAsync(null);

        _remoteSyncController.Verify(x => x.SyncNowAsync(It.IsAny<CancellationToken>()), Times.Once);
        capturedToken.CanBeCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task SyncNowCommand_Should_ResetBusy_And_Not_ShowError_When_Canceled()
    {
        var syncStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _remoteSyncController.Setup(x => x.SyncNowAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async token =>
            {
                syncStarted.TrySetResult(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            });
        var sut = CreateSyncSut();

        var syncTask = sut.SyncNowCommand.ExecuteAsync(null);
        var syncToken = await syncStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        sut.IsSyncControlBusy.Should().BeTrue();

        sut.SyncNowCommand.Cancel();

        syncToken.IsCancellationRequested.Should().BeTrue();
        await syncTask.WaitAsync(TimeSpan.FromSeconds(3));
        sut.IsSyncControlBusy.Should().BeFalse();
        _notificationService.Verify(
            x => x.ShowErrorLocalized(It.IsAny<string>(), It.IsAny<object[]>()),
            Times.Never);
        _notificationService.Verify(x => x.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SyncNowCommand_WhenRemoteSyncEnabledButMisconfigured_Should_ReportConfigurationIssue()
    {
        const string configurationError = "Remote synchronization is enabled, but configuration is incomplete.";
        _remoteSyncController.SetupGet(x => x.IsConfigured).Returns(false);
        _remoteSyncController.SetupGet(x => x.ConfigurationErrorMessage).Returns(configurationError);
        var sut = CreateSyncSut();

        sut.IsRemoteSyncEnabled.Should().BeTrue();
        sut.IsRemoteSyncConfigured.Should().BeFalse();
        sut.SyncNowCommand.CanExecute(null).Should().BeTrue();

        await sut.SyncNowCommand.ExecuteAsync(null);

        _remoteSyncController.Verify(x => x.SyncNowAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(x => x.ShowWarning(configurationError), Times.Once);
        sut.IsSyncControlBusy.Should().BeFalse();
    }

    [Fact]
    public void SyncNowCommand_WhenRemoteSyncDisabled_Should_BeDisabled()
    {
        _remoteSyncController.SetupGet(x => x.IsEnabled).Returns(false);
        _remoteSyncController.SetupGet(x => x.IsConfigured).Returns(false);
        var sut = CreateSyncSut();

        sut.IsRemoteSyncEnabled.Should().BeFalse();
        sut.IsRemoteSyncConfigured.Should().BeFalse();
        sut.SyncNowCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task RemoteSyncEnabledToggle_WhenDisabled_Should_SaveAndRefreshRuntimeState()
    {
        var isRemoteSyncEnabled = true;
        var isRemoteSyncConfigured = true;
        _remoteSyncController.SetupGet(x => x.IsEnabled).Returns(() => isRemoteSyncEnabled);
        _remoteSyncController.SetupGet(x => x.IsConfigured).Returns(() => isRemoteSyncConfigured);
        _remoteSyncController.Setup(x => x.RefreshConfigurationAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                isRemoteSyncEnabled = false;
                isRemoteSyncConfigured = false;
            })
            .Returns(Task.CompletedTask);
        var remoteSyncSettings = new TestRemoteSyncSettingsService(
            new RemoteSyncSettingsSnapshot(true, true, "db.example", 5432, "birds", "user", true));
        var sut = CreateSyncSut(remoteSyncSettings);

        sut.RemoteSyncSettingsEnabled.Should().BeTrue();
        sut.SyncNowCommand.CanExecute(null).Should().BeTrue();

        sut.RemoteSyncSettingsEnabled = false;
        await sut.ApplyRemoteSyncEnabledCommand.ExecuteAsync(null);

        remoteSyncSettings.LastSavedUpdate.Should().NotBeNull();
        remoteSyncSettings.LastSavedUpdate!.IsEnabled.Should().BeFalse();
        _remoteSyncController.Verify(x => x.RefreshConfigurationAsync(It.IsAny<CancellationToken>()), Times.Once);
        sut.IsRemoteSyncEnabled.Should().BeFalse();
        sut.IsRemoteSyncConfigured.Should().BeFalse();
        sut.SyncNowCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task RemoteSyncEnabledToggle_WhenSaveFails_Should_RestoreSavedState()
    {
        const string saveFailure = "Remote sync settings are incomplete.";
        var remoteSyncSettings = new TestRemoteSyncSettingsService(
            new RemoteSyncSettingsSnapshot(true, false, string.Empty, 5432, string.Empty, string.Empty, false))
        {
            SaveResult = RemoteSyncSettingsResult.Failure(saveFailure)
        };
        var sut = CreateSyncSut(remoteSyncSettings);

        sut.RemoteSyncSettingsEnabled.Should().BeFalse();

        sut.RemoteSyncSettingsEnabled = true;
        await sut.ApplyRemoteSyncEnabledCommand.ExecuteAsync(null);

        remoteSyncSettings.LastSavedUpdate.Should().NotBeNull();
        remoteSyncSettings.LastSavedUpdate!.IsEnabled.Should().BeTrue();
        sut.RemoteSyncSettingsEnabled.Should().BeFalse();
        _remoteSyncController.Verify(x => x.RefreshConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(x => x.ShowWarning(saveFailure), Times.Once);
    }

    [Fact]
    public async Task RemoteSyncEnabledToggle_WhenPortIsInvalid_Should_RestoreSavedState()
    {
        var remoteSyncSettings = new TestRemoteSyncSettingsService(
            new RemoteSyncSettingsSnapshot(true, false, string.Empty, 5432, string.Empty, string.Empty, false));
        var sut = CreateSyncSut(remoteSyncSettings);

        sut.RemoteSyncSettingsEnabled = true;
        sut.RemoteSyncPort = "not-a-port";
        await sut.ApplyRemoteSyncEnabledCommand.ExecuteAsync(null);

        remoteSyncSettings.LastSavedUpdate.Should().BeNull();
        sut.RemoteSyncSettingsEnabled.Should().BeFalse();
        sut.RemoteSyncPort.Should().Be("5432");
        _remoteSyncController.Verify(x => x.RefreshConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleRemoteSyncPauseCommand_Should_Pause_When_CurrentlyActive()
    {
        var sut = CreateSyncSut();

        await sut.ToggleRemoteSyncPauseCommand.ExecuteAsync(null);

        _remoteSyncController.Verify(x => x.PauseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleRemoteSyncPauseCommand_Should_Resume_When_CurrentlyPaused()
    {
        _remoteSyncStatus.SetState(RemoteSyncDisplayState.Paused, pendingOperationCount: 2);
        var sut = CreateSyncSut();

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

        var sut = CreateSyncSut();
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

        var sut = CreateSyncSut();
        sut.BeginRedownloadRemoteSnapshotCommand.Execute(null);

        await sut.ConfirmRedownloadRemoteSnapshotCommand.ExecuteAsync(null);

        _birdManager.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
        _autoExportCoordinator.Verify(x => x.MarkDirty(), Times.Never);
        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.CannotRedownloadRemoteSnapshot", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmUploadLocalSnapshotToRemoteCommand_Should_PublishCurrentLocalState_ToRemote()
    {
        _store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });

        var sut = CreateSyncSut();
        sut.BeginUploadLocalSnapshotToRemoteCommand.Execute(null);

        await sut.ConfirmUploadLocalSnapshotToRemoteCommand.ExecuteAsync(null);

        sut.IsConfirmingUploadLocalSnapshotToRemote.Should().BeFalse();
        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _remoteSyncController.Verify(x => x.UploadLocalSnapshotToRemoteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(
            x => x.ShowSuccessLocalized("Info.RemoteSnapshotUploaded", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmUploadLocalSnapshotToRemoteCommand_Should_ShowError_WhenRemoteUploadFails()
    {
        _remoteSyncController.Setup(x => x.UploadLocalSnapshotToRemoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSyncSut();
        sut.BeginUploadLocalSnapshotToRemoteCommand.Execute(null);

        await sut.ConfirmUploadLocalSnapshotToRemoteCommand.ExecuteAsync(null);

        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.CannotUploadLocalSnapshotToRemote", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void SyncSettings_LanguageChanged_Should_Update_Localized_Sync_Text()
    {
        _preferences.SelectedSyncInterval = RemoteSyncIntervalPresets.ThirtySeconds;
        var sut = CreateSyncSut();
        var availableSyncIntervals = sut.AvailableSyncIntervals;
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                changedProperties.Add(args.PropertyName!);
        };

        _culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.AvailableSyncIntervals.Should().NotBeSameAs(availableSyncIntervals);
        sut.AvailableSyncIntervals.Single(x => x.Code == RemoteSyncIntervalPresets.ThirtySeconds).DisplayName
            .Should().Be(AppText.Get("Settings.SyncIntervalOption.ThirtySeconds", _culture));
        sut.SelectedSyncIntervalOption.Should().NotBeNull();
        sut.SelectedSyncIntervalOption!.DisplayName
            .Should().Be(AppText.Get("Settings.SyncIntervalOption.ThirtySeconds", _culture));
        sut.SyncIntervalHint.Should().Contain(AppText.Get("Settings.SyncIntervalOption.ThirtySeconds", _culture));
        sut.RemoteSyncStatusLabel.Should().Be(AppText.Get("Settings.SyncStatus.Disabled", _culture));
        changedProperties.Should().Contain(nameof(SyncSettingsViewModel.AvailableSyncIntervals));
        changedProperties.Should().Contain(nameof(SyncSettingsViewModel.SelectedSyncInterval));
        changedProperties.Should().Contain(nameof(SyncSettingsViewModel.SelectedSyncIntervalOption));
        changedProperties.Should().Contain(nameof(SyncSettingsViewModel.SyncIntervalHint));
        changedProperties.Should().Contain(nameof(SyncSettingsViewModel.RemoteSyncStatusLabel));
    }

    [Fact]
    public void SyncSettings_Dispose_Should_Unsubscribe_From_LongLivedEvents()
    {
        var sut = CreateSyncSut();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                changedProperties.Add(args.PropertyName!);
        };

        sut.Dispose();
        _culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);
        _remoteSyncStatus.SetState(RemoteSyncDisplayState.Offline, lastErrorMessage: "offline");
        _store.ReplaceBirds([
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        ]);

        changedProperties.Should().BeEmpty();
    }

    [Fact]
    public void SettingsViewModel_Should_Compose_AppearanceSettings()
    {
        var appearanceSettings = CreateAppearanceSut();
        var sut = CreateSut(appearanceSettings: appearanceSettings);

        sut.AppearanceSettings.Should().BeSameAs(appearanceSettings);
    }

    [Fact]
    public void SettingsViewModel_Should_Compose_DangerZoneSettings_And_Forward_ImportExport_Busy_State()
    {
        var dangerZoneSettings = CreateDangerZoneSut();
        var importExportSettings = CreateImportExportSut();
        var sut = CreateSut(importExportSettings: importExportSettings, dangerZoneSettings: dangerZoneSettings);

        sut.DangerZoneSettings.Should().BeSameAs(dangerZoneSettings);
        dangerZoneSettings.BeginClearBirdRecordsCommand.CanExecute(null).Should().BeTrue();

        importExportSettings.IsTransferBusy = true;

        dangerZoneSettings.BeginClearBirdRecordsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SettingsViewModel_Should_Compose_SyncSettings_And_Forward_Busy_State()
    {
        var syncSettings = CreateSyncSut();
        var importExportSettings = CreateImportExportSut();
        var sut = CreateSut(syncSettings, importExportSettings);

        sut.SyncSettings.Should().BeSameAs(syncSettings);
        syncSettings.SyncNowCommand.CanExecute(null).Should().BeTrue();

        importExportSettings.IsTransferBusy = true;

        syncSettings.SyncNowCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SettingsViewModel_Should_Compose_ImportExportSettings_And_Forward_Danger_Busy_State()
    {
        var dangerZoneSettings = CreateDangerZoneSut();
        var importExportSettings = CreateImportExportSut();
        var sut = CreateSut(importExportSettings: importExportSettings, dangerZoneSettings: dangerZoneSettings);

        sut.ImportExportSettings.Should().BeSameAs(importExportSettings);
        importExportSettings.ExportDataCommand.CanExecute(null).Should().BeTrue();

        dangerZoneSettings.IsDangerZoneBusy = true;

        importExportSettings.ExportDataCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SettingsViewModel_Should_Close_Danger_Confirmation_When_Sync_Confirmation_Starts()
    {
        var syncSettings = CreateSyncSut();
        var dangerZoneSettings = CreateDangerZoneSut();
        var sut = CreateSut(syncSettings: syncSettings, dangerZoneSettings: dangerZoneSettings);
        dangerZoneSettings.BeginClearBirdRecordsCommand.Execute(null);

        syncSettings.BeginUploadLocalSnapshotToRemoteCommand.Execute(null);

        dangerZoneSettings.IsConfirmingClearBirdRecords.Should().BeFalse();
        syncSettings.IsConfirmingUploadLocalSnapshotToRemote.Should().BeTrue();
    }

    [Fact]
    public void SettingsViewModel_Should_Close_Sync_Confirmation_When_Danger_Confirmation_Starts()
    {
        var syncSettings = CreateSyncSut();
        var dangerZoneSettings = CreateDangerZoneSut();
        var sut = CreateSut(syncSettings: syncSettings, dangerZoneSettings: dangerZoneSettings);
        syncSettings.BeginUploadLocalSnapshotToRemoteCommand.Execute(null);

        dangerZoneSettings.BeginClearBirdRecordsCommand.Execute(null);

        syncSettings.IsConfirmingUploadLocalSnapshotToRemote.Should().BeFalse();
        dangerZoneSettings.IsConfirmingClearBirdRecords.Should().BeTrue();
    }

    [Fact]
    public void SettingsViewModel_Dispose_Should_Dispose_Composed_Settings()
    {
        var appearanceSettings = CreateAppearanceSut();
        var syncSettings = CreateSyncSut();
        var importExportSettings = CreateImportExportSut();
        var dangerZoneSettings = CreateDangerZoneSut();
        var sut = CreateSut(syncSettings, importExportSettings, appearanceSettings, dangerZoneSettings);
        sut.Dispose();
        var appearanceChangedProperties = new List<string>();
        appearanceSettings.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                appearanceChangedProperties.Add(args.PropertyName!);
        };
        var syncChangedProperties = new List<string>();
        syncSettings.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                syncChangedProperties.Add(args.PropertyName!);
        };
        var importExportChangedProperties = new List<string>();
        importExportSettings.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                importExportChangedProperties.Add(args.PropertyName!);
        };

        _remoteSyncStatus.SetState(RemoteSyncDisplayState.Offline, lastErrorMessage: "offline");
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.AutoExportEnabled = false;
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);
        dangerZoneSettings.IsDangerZoneBusy = true;

        appearanceChangedProperties.Should().BeEmpty();
        syncChangedProperties.Should().BeEmpty();
        importExportChangedProperties.Should().BeEmpty();
        importExportSettings.ExportDataCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void LanguageChanged_Should_Preserve_SelectedTheme_And_Reapply_It()
    {
        _preferences.SelectedLanguage = AppLanguages.Russian;
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.SelectedSyncInterval = RemoteSyncIntervalPresets.ThirtySeconds;

        var sut = CreateAppearanceSut();
        var availableThemes = sut.AvailableThemes;
        var availableLanguages = sut.AvailableLanguages;
        var availableDateFormats = sut.AvailableDateFormats;
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                changedProperties.Add(args.PropertyName!);
        };

        _culture = CultureInfo.GetCultureInfo(AppLanguages.English);
        _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

        sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
        _preferences.SelectedTheme.Should().Be(ThemeKeys.Steel);
        sut.AvailableThemes.Should().NotBeSameAs(availableThemes);
        sut.AvailableLanguages.Should().NotBeSameAs(availableLanguages);
        sut.AvailableDateFormats.Should().NotBeSameAs(availableDateFormats);
        sut.AvailableThemes.Single(x => x.Code == ThemeKeys.Steel).DisplayName
            .Should().Be(AppText.Get("Settings.Theme.Steel", _culture));
        sut.AvailableLanguages.Single(x => x.Code == AppLanguages.English).DisplayName
            .Should().Be(AppText.Get("Language.English", _culture));
        sut.AvailableDateFormats.Single(x => x.Code == DateDisplayFormats.DayMonthYear).DisplayName
            .Should().Be(AppText.Get("Settings.DateFormat.DayMonthYear", _culture));
        sut.SelectedThemeOption.Should().NotBeNull();
        sut.SelectedThemeOption!.DisplayName.Should().Be(AppText.Get("Settings.Theme.Steel", _culture));
        sut.SelectedLanguageOption.Should().NotBeNull();
        sut.SelectedLanguageOption!.DisplayName.Should().Be(AppText.Get("Language.Russian", _culture));
        sut.SelectedDateFormatOption.Should().NotBeNull();
        sut.SelectedDateFormatOption!.DisplayName.Should()
            .Be(AppText.Get("Settings.DateFormat.DayMonthYear", _culture));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.AvailableThemes));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.AvailableLanguages));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.AvailableDateFormats));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.SelectedTheme));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.SelectedLanguage));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.SelectedDateFormat));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.SelectedThemeOption));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.SelectedLanguageOption));
        changedProperties.Should().Contain(nameof(AppearanceSettingsViewModel.SelectedDateFormatOption));
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

        var sut = CreateImportExportSut();

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
    public async Task ExportDataCommand_Should_ClearBusy_And_ShowError_WhenExportFails()
    {
        _exportService.Setup(x => x.ExportAsync(
                It.IsAny<IEnumerable<BirdDTO>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk full"));
        var sut = CreateImportExportSut();

        await sut.ExportDataCommand.ExecuteAsync(null);

        sut.IsTransferBusy.Should().BeFalse();
        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.ExportFailed", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void ChooseExportPathCommand_Should_Persist_Selected_Path()
    {
        _dataFileDialogService.Setup(x => x.PickExportPath(It.IsAny<string>()))
            .Returns("C:\\exports\\selected-birds.json");

        var sut = CreateImportExportSut();

        sut.ChooseExportPathCommand.Execute(null);

        _preferences.CustomExportPath.Should().Be("C:\\exports\\selected-birds.json");
        sut.ExportPathHint.Should().Contain("selected-birds.json");
    }

    [Fact]
    public void OpenExportFolderCommand_Should_Open_Directory_For_Current_Export_Path()
    {
        _preferences.CustomExportPath = "C:\\exports\\selected-birds.json";

        var sut = CreateImportExportSut();

        sut.OpenExportFolderCommand.Execute(null);

        _pathNavigationService.Verify(x => x.OpenDirectory("C:\\exports"), Times.Once);
    }

    [Fact]
    public void OpenExportFileCommand_Should_Open_Current_Export_File()
    {
        _preferences.CustomExportPath = "C:\\exports\\selected-birds.json";

        var sut = CreateImportExportSut();

        sut.OpenExportFileCommand.Execute(null);

        _pathNavigationService.Verify(x => x.OpenFile("C:\\exports\\selected-birds.json"), Times.Once);
    }

    [Fact]
    public void OpenExportFileCommand_Should_ShowError_When_OpenFails()
    {
        _pathNavigationService.Setup(x => x.OpenFile(It.IsAny<string>())).Returns(false);

        var sut = CreateImportExportSut();

        sut.OpenExportFileCommand.Execute(null);

        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.CannotOpenExportFile", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void OpenExportFolderCommand_Should_ShowError_When_OpenFails()
    {
        _pathNavigationService.Setup(x => x.OpenDirectory(It.IsAny<string>())).Returns(false);

        var sut = CreateImportExportSut();

        sut.OpenExportFolderCommand.Execute(null);

        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.CannotOpenExportFolder", It.IsAny<object[]>()),
            Times.Once);
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

        var sut = CreateImportExportSut();

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

        var sut = CreateImportExportSut();

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

        var sut = CreateDangerZoneSut();
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

        var sut = CreateDangerZoneSut();
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

    [Fact]
    public async Task ConfirmClearBirdRecordsCommand_Should_Not_Run_Without_Confirmation()
    {
        var sut = CreateDangerZoneSut();

        sut.ConfirmClearBirdRecordsCommand.CanExecute(null).Should().BeFalse();
        await sut.ConfirmClearBirdRecordsCommand.ExecuteAsync(null);

        _databaseMaintenanceService.Verify(x => x.ClearBirdRecordsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmResetLocalDatabaseCommand_Should_Not_Run_Without_Confirmation()
    {
        var sut = CreateDangerZoneSut();

        sut.ConfirmResetLocalDatabaseCommand.CanExecute(null).Should().BeFalse();
        await sut.ConfirmResetLocalDatabaseCommand.ExecuteAsync(null);

        _databaseMaintenanceService.Verify(x => x.ResetLocalDatabaseAsync(It.IsAny<CancellationToken>()), Times.Never);
        _birdManager.Verify(x => x.FlushPendingOperationsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void DangerZoneCommands_Should_Be_Disabled_While_External_Busy()
    {
        var sut = CreateDangerZoneSut();

        sut.SetExternalBusy(true);

        sut.BeginClearBirdRecordsCommand.CanExecute(null).Should().BeFalse();
        sut.BeginResetLocalDatabaseCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmClearBirdRecordsCommand_Should_ClearBusy_And_ShowError_When_ClearFails()
    {
        _databaseMaintenanceService.Setup(x => x.ClearBirdRecordsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("locked"));
        var sut = CreateDangerZoneSut();
        sut.BeginClearBirdRecordsCommand.Execute(null);

        await sut.ConfirmClearBirdRecordsCommand.ExecuteAsync(null);

        sut.IsDangerZoneBusy.Should().BeFalse();
        sut.IsConfirmingClearBirdRecords.Should().BeTrue();
        _notificationService.Verify(
            x => x.ShowErrorLocalized("Error.CannotClearBirdRecords", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void ResetPreferencesCommand_Should_Reset_Preferences()
    {
        _preferences.SelectedTheme = ThemeKeys.Steel;
        _preferences.AutoExportEnabled = true;
        var sut = CreateDangerZoneSut();

        sut.ResetPreferencesCommand.Execute(null);

        _preferences.SelectedTheme.Should().Be(AppPreferencesState.DefaultTheme);
        _preferences.AutoExportEnabled.Should().Be(AppPreferencesState.DefaultAutoExportEnabled);
    }

    private SettingsViewModel CreateSut(
        SyncSettingsViewModel? syncSettings = null,
        ImportExportSettingsViewModel? importExportSettings = null,
        AppearanceSettingsViewModel? appearanceSettings = null,
        DangerZoneSettingsViewModel? dangerZoneSettings = null)
    {
        return new SettingsViewModel(
            appearanceSettings ?? CreateAppearanceSut(),
            importExportSettings ?? CreateImportExportSut(),
            syncSettings ?? CreateSyncSut(),
            dangerZoneSettings ?? CreateDangerZoneSut());
    }

    private AppearanceSettingsViewModel CreateAppearanceSut()
    {
        return new AppearanceSettingsViewModel(
            _preferences,
            _themeService.Object,
            _localization.Object,
            _birdManager.Object,
            TestBackgroundTaskRunner.Create());
    }

    private ImportExportSettingsViewModel CreateImportExportSut()
    {
        return new ImportExportSettingsViewModel(
            _preferences,
            _localization.Object,
            _birdManager.Object,
            _exportService.Object,
            _exportPathProvider.Object,
            _autoExportCoordinator.Object,
            _importService.Object,
            _dataFileDialogService.Object,
            _pathNavigationService.Object,
            _notificationService.Object,
            _mediator.Object);
    }

    private SyncSettingsViewModel CreateSyncSut(IRemoteSyncSettingsService? remoteSyncSettingsService = null)
    {
        return new SyncSettingsViewModel(
            _preferences,
            _localization.Object,
            _birdManager.Object,
            _autoExportCoordinator.Object,
            _notificationService.Object,
            _remoteSyncStatus,
            _remoteSyncController.Object,
            remoteSyncSettingsService ?? new TestRemoteSyncSettingsService(
                new RemoteSyncSettingsSnapshot(false, false, string.Empty, AppPreferencesState.DefaultRemoteSyncPort, string.Empty, string.Empty, false)));
    }

    private DangerZoneSettingsViewModel CreateDangerZoneSut()
    {
        return new DangerZoneSettingsViewModel(
            _preferences,
            _birdManager.Object,
            _autoExportCoordinator.Object,
            _notificationService.Object,
            _databaseMaintenanceService.Object);
    }

    private sealed class TestPreferencesService : IAppPreferencesService
    {
        private bool _autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
        private string _customExportPath = string.Empty;
        private string _selectedDateFormat = DateDisplayFormats.DayMonthYear;
        private string _selectedImportMode = BirdImportModes.Merge;
        private string _selectedSyncInterval = AppPreferencesState.DefaultSyncInterval;
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

        public string SelectedSyncInterval
        {
            get => _selectedSyncInterval;
            set
            {
                if (_selectedSyncInterval == value)
                    return;
                _selectedSyncInterval = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSyncInterval)));
            }
        }

        public bool RemoteSyncConfigurationSaved { get; set; } = AppPreferencesState.DefaultRemoteSyncConfigurationSaved;

        public bool RemoteSyncEnabled { get; set; } = AppPreferencesState.DefaultRemoteSyncEnabled;

        public string RemoteSyncHost { get; set; } = string.Empty;

        public int RemoteSyncPort { get; set; } = AppPreferencesState.DefaultRemoteSyncPort;

        public string RemoteSyncDatabase { get; set; } = string.Empty;

        public string RemoteSyncUsername { get; set; } = string.Empty;

        public void ResetToDefaults()
        {
            SelectedLanguage = AppPreferencesState.DefaultLanguage;
            SelectedTheme = AppPreferencesState.DefaultTheme;
            SelectedDateFormat = AppPreferencesState.DefaultDateFormat;
            SelectedImportMode = AppPreferencesState.DefaultImportMode;
            SelectedSyncInterval = AppPreferencesState.DefaultSyncInterval;
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

        public RemoteSyncSnapshotState RemoteSnapshotState { get; private set; } = RemoteSyncSnapshotState.Unknown;

        public int? RemoteBirdCount { get; private set; }

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
            RemoteSyncSnapshotState remoteSnapshotState = RemoteSyncSnapshotState.Unknown,
            int? remoteBirdCount = null,
            IReadOnlyList<RemoteSyncActivityEntry>? recentActivity = null)
        {
            Status = status;
            RemoteSnapshotState = remoteSnapshotState;
            RemoteBirdCount = remoteBirdCount;
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemoteSnapshotState)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemoteBirdCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastSuccessfulSyncAtUtc)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastAttemptAtUtc)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastErrorMessage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastProcessedCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingOperationCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentActivity)));
        }
    }

    private sealed class TestRemoteSyncSettingsService(RemoteSyncSettingsSnapshot snapshot) : IRemoteSyncSettingsService
    {
        private RemoteSyncSettingsSnapshot _snapshot = snapshot;

        public RemoteSyncSettingsUpdate? LastSavedUpdate { get; private set; }

        public RemoteSyncSettingsResult SaveResult { get; set; } =
            RemoteSyncSettingsResult.Success("Remote sync settings saved.");

        public RemoteSyncSettingsSnapshot GetSnapshot()
        {
            return _snapshot;
        }

        public Task<RemoteSyncSettingsResult> SaveAsync(
            RemoteSyncSettingsUpdate update,
            CancellationToken cancellationToken)
        {
            LastSavedUpdate = update;
            if (SaveResult.IsSuccess)
            {
                _snapshot = _snapshot with
                {
                    IsUserConfigured = true,
                    IsEnabled = update.IsEnabled,
                    Host = update.Host,
                    Port = update.Port,
                    Database = update.Database,
                    Username = update.Username,
                    HasSavedPassword = _snapshot.HasSavedPassword || !string.IsNullOrWhiteSpace(update.Password)
                };
            }

            return Task.FromResult(SaveResult);
        }

        public Task<RemoteSyncSettingsResult> TestConnectionAsync(
            RemoteSyncSettingsUpdate update,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(RemoteSyncSettingsResult.Success("Connection succeeded."));
        }
    }
}
