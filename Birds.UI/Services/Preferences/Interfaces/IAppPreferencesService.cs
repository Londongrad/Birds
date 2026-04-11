using System.ComponentModel;

namespace Birds.UI.Services.Preferences.Interfaces;

public interface IAppPreferencesService : INotifyPropertyChanged
{
    string SelectedLanguage { get; set; }

    string SelectedTheme { get; set; }

    string SelectedDateFormat { get; set; }

    string SelectedImportMode { get; set; }

    string CustomExportPath { get; set; }

    bool AutoExportEnabled { get; set; }

    bool ShowNotificationBadge { get; set; }

    bool ShowSyncStatusIndicator { get; set; }

    void ResetToDefaults();
}