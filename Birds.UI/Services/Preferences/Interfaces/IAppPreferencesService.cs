using System.ComponentModel;

namespace Birds.UI.Services.Preferences.Interfaces
{
    public interface IAppPreferencesService : INotifyPropertyChanged
    {
        string SelectedLanguage { get; set; }

        string SelectedTheme { get; set; }

        string SelectedDateFormat { get; set; }

        string SelectedImportMode { get; set; }

        bool ShowNotificationBadge { get; set; }

        bool ReduceMotion { get; set; }

        void ResetToDefaults();
    }
}
