using System.ComponentModel;
using Birds.Application.DTOs;
using Birds.Tests.Helpers;
using Birds.UI.Services.Background;
using Birds.UI.Services.Export;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Import;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Services.Theming;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Birds.Tests.UI.Services;

public sealed class AutoExportCoordinatorTests
{
    [Fact]
    public async Task MarkDirty_Should_Debounce_And_Export_Latest_Snapshot_To_Custom_Path()
    {
        var store = new BirdStore();
        var exportService = new Mock<IExportService>();
        var exportPathProvider = new Mock<IExportPathProvider>();
        var preferences = new TestPreferencesService
        {
            CustomExportPath = "C:\\exports\\birds-auto.json"
        };

        store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });

        using var sut = new AutoExportCoordinator(
            store,
            exportService.Object,
            exportPathProvider.Object,
            preferences,
            new InlineUiDispatcher(),
            NullLogger<AutoExportCoordinator>.Instance,
            CreateBackgroundTaskRunner(),
            TimeSpan.FromMilliseconds(40));

        sut.MarkDirty();

        store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null),
            new BirdDTO(Guid.NewGuid(), "Great tit", null, new DateOnly(2026, 4, 2), null, true, null, null)
        });
        sut.MarkDirty();

        await Task.Delay(120);

        exportService.Verify(
            x => x.ExportAsync(
                It.Is<IEnumerable<BirdDTO>>(items => items.Count() == 2),
                "C:\\exports\\birds-auto.json",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FlushAsync_Should_Export_Immediately_Without_Waiting_For_Debounce()
    {
        var store = new BirdStore();
        var exportService = new Mock<IExportService>();
        var exportPathProvider = new Mock<IExportPathProvider>();
        exportPathProvider.Setup(x => x.GetLatestPath("birds", It.IsAny<string>()))
            .Returns("C:\\exports\\birds.json");

        store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });

        using var sut = new AutoExportCoordinator(
            store,
            exportService.Object,
            exportPathProvider.Object,
            new TestPreferencesService(),
            new InlineUiDispatcher(),
            NullLogger<AutoExportCoordinator>.Instance,
            CreateBackgroundTaskRunner(),
            TimeSpan.FromSeconds(5));

        sut.MarkDirty();
        await sut.FlushAsync(CancellationToken.None);
        await Task.Delay(100);

        exportService.Verify(
            x => x.ExportAsync(
                It.IsAny<IEnumerable<BirdDTO>>(),
                "C:\\exports\\birds.json",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkDirty_WhenAutoExportIsDisabled_Should_NotExport_Until_Reenabled()
    {
        var store = new BirdStore();
        var exportService = new Mock<IExportService>();
        var exportPathProvider = new Mock<IExportPathProvider>();
        exportPathProvider.Setup(x => x.GetLatestPath("birds", It.IsAny<string>()))
            .Returns("C:\\exports\\birds.json");

        store.ReplaceBirds(new[]
        {
            new BirdDTO(Guid.NewGuid(), "Sparrow", null, new DateOnly(2026, 4, 1), null, true, null, null)
        });

        var preferences = new TestPreferencesService
        {
            AutoExportEnabled = false
        };

        using var sut = new AutoExportCoordinator(
            store,
            exportService.Object,
            exportPathProvider.Object,
            preferences,
            new InlineUiDispatcher(),
            NullLogger<AutoExportCoordinator>.Instance,
            CreateBackgroundTaskRunner(),
            TimeSpan.FromMilliseconds(40));

        sut.MarkDirty();
        await Task.Delay(120);

        exportService.Verify(
            x => x.ExportAsync(
                It.IsAny<IEnumerable<BirdDTO>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        preferences.AutoExportEnabled = true;
        await Task.Delay(120);

        exportService.Verify(
            x => x.ExportAsync(
                It.Is<IEnumerable<BirdDTO>>(items => items.Count() == 1),
                "C:\\exports\\birds.json",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestPreferencesService : IAppPreferencesService
    {
        private bool _autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
        private string _customExportPath = string.Empty;
        private bool _reduceMotion;
        private string _selectedDateFormat = DateDisplayFormats.Default;
        private string _selectedImportMode = BirdImportModes.Merge;
        private string _selectedSyncInterval = AppPreferencesState.DefaultSyncInterval;
        private string _selectedLanguage = AppPreferencesState.DefaultLanguage;
        private string _selectedTheme = ThemeKeys.Graphite;
        private bool _showNotificationBadge = true;
        private bool _showSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;

        public bool ReduceMotion
        {
            get => _reduceMotion;
            set => SetField(ref _reduceMotion, value, nameof(ReduceMotion));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetField(ref _selectedLanguage, value, nameof(SelectedLanguage));
        }

        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetField(ref _selectedTheme, value, nameof(SelectedTheme));
        }

        public string SelectedDateFormat
        {
            get => _selectedDateFormat;
            set => SetField(ref _selectedDateFormat, value, nameof(SelectedDateFormat));
        }

        public string SelectedImportMode
        {
            get => _selectedImportMode;
            set => SetField(ref _selectedImportMode, value, nameof(SelectedImportMode));
        }

        public string SelectedSyncInterval
        {
            get => _selectedSyncInterval;
            set => SetField(ref _selectedSyncInterval, value, nameof(SelectedSyncInterval));
        }

        public bool RemoteSyncConfigurationSaved { get; set; } = AppPreferencesState.DefaultRemoteSyncConfigurationSaved;

        public bool RemoteSyncEnabled { get; set; } = AppPreferencesState.DefaultRemoteSyncEnabled;

        public string RemoteSyncHost { get; set; } = string.Empty;

        public int RemoteSyncPort { get; set; } = AppPreferencesState.DefaultRemoteSyncPort;

        public string RemoteSyncDatabase { get; set; } = string.Empty;

        public string RemoteSyncUsername { get; set; } = string.Empty;

        public string CustomExportPath
        {
            get => _customExportPath;
            set => SetField(ref _customExportPath, value, nameof(CustomExportPath));
        }

        public bool AutoExportEnabled
        {
            get => _autoExportEnabled;
            set => SetField(ref _autoExportEnabled, value, nameof(AutoExportEnabled));
        }

        public bool ShowNotificationBadge
        {
            get => _showNotificationBadge;
            set => SetField(ref _showNotificationBadge, value, nameof(ShowNotificationBadge));
        }

        public bool ShowSyncStatusIndicator
        {
            get => _showSyncStatusIndicator;
            set => SetField(ref _showSyncStatusIndicator, value, nameof(ShowSyncStatusIndicator));
        }

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
            ReduceMotion = false;
        }

        private void SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private static IBackgroundTaskRunner CreateBackgroundTaskRunner()
    {
        return TestBackgroundTaskRunner.Create();
    }
}
