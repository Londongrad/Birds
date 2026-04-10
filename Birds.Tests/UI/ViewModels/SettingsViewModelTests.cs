using Birds.Application.Commands.ImportBirds;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
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
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using MediatR;
using Moq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace Birds.Tests.UI.ViewModels
{
    public class SettingsViewModelTests
    {
        private readonly TestPreferencesService _preferences = new();
        private readonly Mock<IThemeService> _themeService = new();
        private readonly Mock<ILocalizationService> _localization = new();
        private readonly Mock<IBirdManager> _birdManager = new();
        private readonly Mock<IExportService> _exportService = new();
        private readonly Mock<IExportPathProvider> _exportPathProvider = new();
        private readonly Mock<IAutoExportCoordinator> _autoExportCoordinator = new();
        private readonly Mock<IImportService> _importService = new();
        private readonly Mock<IDataFileDialogService> _dataFileDialogService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IDatabaseMaintenanceService> _databaseMaintenanceService = new();
        private readonly BirdStore _store = new();
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
            _preferences.ReduceMotion = false;

            var sut = CreateSut();

            sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Graphite", _culture));
            sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.DayMonthYear", _culture));
            sut.ImportModeHint.Should().Be(AppText.Get("Settings.ImportModeHint.Merge", _culture));
            sut.AutoExportHint.Should().Be(AppText.Get("Settings.AutoExportHint.Enabled", _culture));
            sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Enabled", _culture));
            sut.MotionHint.Should().Be(AppText.Get("Settings.MotionHint.Disabled", _culture));
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
            _preferences.ReduceMotion = true;

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
            sut.MotionHint.Should().Be(AppText.Get("Settings.MotionHint.Enabled", _culture));
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
                x => x.ShowSuccessLocalized("Info.ExportSucceeded", It.Is<object[]>(args => args.Single().Equals("C:\\temp\\birds-export.json"))),
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
            var importedBird = new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null);

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
                    It.Is<object[]>(args => args.Length == 3 && (int)args[0] == 1 && (int)args[1] == 1 && (int)args[2] == 0)),
                Times.Once);
        }

        [Fact]
        public async Task ImportDataCommand_Should_Use_Replace_Mode_When_Selected()
        {
            var importedBird = new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null);

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
                x => x.ShowSuccessLocalized("Info.BirdRecordsCleared", It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 1)),
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
            _notificationService.Verify(x => x.ShowSuccessLocalized("Info.LocalDatabaseReset", It.IsAny<object[]>()), Times.Once);
        }

        private SettingsViewModel CreateSut()
            => new(
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
                _databaseMaintenanceService.Object);

        private sealed class TestPreferencesService : IAppPreferencesService
        {
            private string _selectedLanguage = AppLanguages.Russian;
            private string _selectedTheme = ThemeKeys.Graphite;
            private string _selectedDateFormat = DateDisplayFormats.DayMonthYear;
            private string _selectedImportMode = BirdImportModes.Merge;
            private string _customExportPath = string.Empty;
            private bool _autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
            private bool _showNotificationBadge = true;
            private bool _reduceMotion;

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

            public bool ReduceMotion
            {
                get => _reduceMotion;
                set
                {
                    if (_reduceMotion == value)
                        return;
                    _reduceMotion = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReduceMotion)));
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
                ReduceMotion = false;
            }
        }
    }
}
