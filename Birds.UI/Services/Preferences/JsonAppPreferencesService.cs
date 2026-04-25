using System.IO;
using System.Text.Json;
using Birds.Shared.Localization;
using Birds.Shared.Sync;
using Birds.UI.Services.Import;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Birds.UI.Services.Preferences;

public sealed partial class JsonAppPreferencesService : ObservableObject, IAppPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IAppPreferencesPathProvider _pathProvider;
    private readonly ILogger<JsonAppPreferencesService> _logger;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    [ObservableProperty] private bool autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;

    [ObservableProperty] private string customExportPath = string.Empty;

    [ObservableProperty] private string selectedDateFormat = AppPreferencesState.DefaultDateFormat;

    [ObservableProperty] private string selectedImportMode = AppPreferencesState.DefaultImportMode;

    [ObservableProperty] private string selectedSyncInterval = AppPreferencesState.DefaultSyncInterval;

    [ObservableProperty] private bool remoteSyncConfigurationSaved = AppPreferencesState.DefaultRemoteSyncConfigurationSaved;

    [ObservableProperty] private bool remoteSyncEnabled = AppPreferencesState.DefaultRemoteSyncEnabled;

    [ObservableProperty] private string remoteSyncHost = string.Empty;

    [ObservableProperty] private int remoteSyncPort = AppPreferencesState.DefaultRemoteSyncPort;

    [ObservableProperty] private string remoteSyncDatabase = string.Empty;

    [ObservableProperty] private string remoteSyncUsername = string.Empty;

    [ObservableProperty] private string selectedLanguage = AppPreferencesState.DefaultLanguage;

    [ObservableProperty] private string selectedTheme = AppPreferencesState.DefaultTheme;

    [ObservableProperty] private bool showNotificationBadge = true;

    [ObservableProperty] private bool showSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;

    public JsonAppPreferencesService(
        IAppPreferencesPathProvider pathProvider,
        ILogger<JsonAppPreferencesService> logger)
    {
        _pathProvider = pathProvider;
        _logger = logger;

        var state = LoadState(out var shouldPersistDefaults);
        selectedLanguage = AppLanguages.Normalize(state.SelectedLanguage);
        selectedTheme = ThemeKeys.Normalize(state.SelectedTheme);
        selectedDateFormat = DateDisplayFormats.Normalize(state.SelectedDateFormat);
        selectedImportMode = BirdImportModes.Normalize(state.SelectedImportMode);
        selectedSyncInterval = RemoteSyncIntervalPresets.Normalize(state.SelectedSyncInterval);
        remoteSyncConfigurationSaved = state.RemoteSyncConfigurationSaved;
        remoteSyncEnabled = state.RemoteSyncEnabled;
        remoteSyncHost = state.RemoteSyncHost?.Trim() ?? string.Empty;
        remoteSyncPort = state.RemoteSyncPort > 0
            ? state.RemoteSyncPort
            : AppPreferencesState.DefaultRemoteSyncPort;
        remoteSyncDatabase = state.RemoteSyncDatabase?.Trim() ?? string.Empty;
        remoteSyncUsername = state.RemoteSyncUsername?.Trim() ?? string.Empty;
        customExportPath = state.CustomExportPath?.Trim() ?? string.Empty;
        autoExportEnabled = state.AutoExportEnabled;
        showNotificationBadge = state.ShowNotificationBadge;
        showSyncStatusIndicator = state.ShowSyncStatusIndicator;

        if (shouldPersistDefaults)
            SaveState();
    }

    public void ResetToDefaults()
    {
        SelectedLanguage = AppPreferencesState.DefaultLanguage;
        SelectedTheme = AppPreferencesState.DefaultTheme;
        SelectedDateFormat = AppPreferencesState.DefaultDateFormat;
        SelectedImportMode = AppPreferencesState.DefaultImportMode;
        SelectedSyncInterval = AppPreferencesState.DefaultSyncInterval;
        RemoteSyncConfigurationSaved = AppPreferencesState.DefaultRemoteSyncConfigurationSaved;
        RemoteSyncEnabled = AppPreferencesState.DefaultRemoteSyncEnabled;
        RemoteSyncHost = string.Empty;
        RemoteSyncPort = AppPreferencesState.DefaultRemoteSyncPort;
        RemoteSyncDatabase = string.Empty;
        RemoteSyncUsername = string.Empty;
        CustomExportPath = string.Empty;
        AutoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
        ShowNotificationBadge = true;
        ShowSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        SaveState();
    }

    partial void OnSelectedThemeChanged(string value)
    {
        selectedTheme = ThemeKeys.Normalize(value);
        SaveState();
    }

    partial void OnSelectedDateFormatChanged(string value)
    {
        selectedDateFormat = DateDisplayFormats.Normalize(value);
        SaveState();
    }

    partial void OnSelectedImportModeChanged(string value)
    {
        selectedImportMode = BirdImportModes.Normalize(value);
        SaveState();
    }

    partial void OnSelectedSyncIntervalChanged(string value)
    {
        selectedSyncInterval = RemoteSyncIntervalPresets.Normalize(value);
        SaveState();
    }

    partial void OnRemoteSyncConfigurationSavedChanged(bool value)
    {
        SaveState();
    }

    partial void OnRemoteSyncEnabledChanged(bool value)
    {
        SaveState();
    }

    partial void OnRemoteSyncHostChanged(string value)
    {
        remoteSyncHost = value?.Trim() ?? string.Empty;
        SaveState();
    }

    partial void OnRemoteSyncPortChanged(int value)
    {
        remoteSyncPort = value > 0
            ? value
            : AppPreferencesState.DefaultRemoteSyncPort;
        SaveState();
    }

    partial void OnRemoteSyncDatabaseChanged(string value)
    {
        remoteSyncDatabase = value?.Trim() ?? string.Empty;
        SaveState();
    }

    partial void OnRemoteSyncUsernameChanged(string value)
    {
        remoteSyncUsername = value?.Trim() ?? string.Empty;
        SaveState();
    }

    partial void OnCustomExportPathChanged(string value)
    {
        customExportPath = value?.Trim() ?? string.Empty;
        SaveState();
    }

    partial void OnAutoExportEnabledChanged(bool value)
    {
        SaveState();
    }

    partial void OnShowNotificationBadgeChanged(bool value)
    {
        SaveState();
    }

    partial void OnShowSyncStatusIndicatorChanged(bool value)
    {
        SaveState();
    }

    private AppPreferencesState LoadState(out bool shouldPersistDefaults)
    {
        shouldPersistDefaults = false;
        var path = _pathProvider.GetPreferencesPath();

        try
        {
            if (!File.Exists(path))
                return new AppPreferencesState();

            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<AppPreferencesState>(json, JsonOptions);
            if (state is not null)
                return state;

            BackupBrokenPreferences(path, "deserialized preferences were null");
            shouldPersistDefaults = true;
            return new AppPreferencesState();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to load preferences from {Path}. The file appears to be malformed.", path);
            BackupBrokenPreferences(path, "malformed JSON");
            shouldPersistDefaults = true;
            return new AppPreferencesState();
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Failed to deserialize preferences from {Path}.", path);
            BackupBrokenPreferences(path, "unsupported JSON shape");
            shouldPersistDefaults = true;
            return new AppPreferencesState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preferences from {Path}. Defaults will be used.", path);
            return new AppPreferencesState();
        }
    }

    private void SaveState()
    {
        var path = _pathProvider.GetPreferencesPath();
        var tempPath = CreateTempPath(path);

        _saveLock.Wait();
        try
        {
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(
                new AppPreferencesState
                {
                    SelectedLanguage = SelectedLanguage,
                    SelectedTheme = ThemeKeys.Normalize(SelectedTheme),
                    SelectedDateFormat = DateDisplayFormats.Normalize(SelectedDateFormat),
                    SelectedImportMode = BirdImportModes.Normalize(SelectedImportMode),
                    SelectedSyncInterval = RemoteSyncIntervalPresets.Normalize(SelectedSyncInterval),
                    RemoteSyncConfigurationSaved = RemoteSyncConfigurationSaved,
                    RemoteSyncEnabled = RemoteSyncEnabled,
                    RemoteSyncHost = RemoteSyncHost?.Trim() ?? string.Empty,
                    RemoteSyncPort = RemoteSyncPort > 0
                        ? RemoteSyncPort
                        : AppPreferencesState.DefaultRemoteSyncPort,
                    RemoteSyncDatabase = RemoteSyncDatabase?.Trim() ?? string.Empty,
                    RemoteSyncUsername = RemoteSyncUsername?.Trim() ?? string.Empty,
                    CustomExportPath = CustomExportPath?.Trim() ?? string.Empty,
                    AutoExportEnabled = AutoExportEnabled,
                    ShowNotificationBadge = ShowNotificationBadge,
                    ShowSyncStatusIndicator = ShowSyncStatusIndicator
                },
                JsonOptions);

            WriteAllTextDurably(tempPath, json);
            ReplacePreferencesFile(tempPath, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save preferences to {Path}.", path);
        }
        finally
        {
            TryDeleteTempFile(tempPath);
            _saveLock.Release();
        }
    }

    private void BackupBrokenPreferences(string path, string reason)
    {
        try
        {
            if (!File.Exists(path))
                return;

            var backupPath = CreateBrokenBackupPath(path);
            File.Copy(path, backupPath, overwrite: false);
            _logger.LogWarning(
                "Backed up broken preferences file {Path} to {BackupPath}. Reason: {Reason}.",
                path,
                backupPath,
                reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to back up broken preferences file {Path}.", path);
        }
    }

    private static string CreateBrokenBackupPath(string path)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var candidate = $"{path}.broken-{timestamp}";
        var counter = 0;

        while (File.Exists(candidate))
            candidate = $"{path}.broken-{timestamp}-{++counter}";

        return candidate;
    }

    private static string CreateTempPath(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp";

        return string.IsNullOrWhiteSpace(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }

    private static void WriteAllTextDurably(string path, string contents)
    {
        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        using var writer = new StreamWriter(stream);
        writer.Write(contents);
    }

    private static void ReplacePreferencesFile(string tempPath, string path)
    {
        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
            return;
        }

        File.Move(tempPath, path);
    }

    private void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temporary preferences file {TempPath}.", tempPath);
        }
    }
}
