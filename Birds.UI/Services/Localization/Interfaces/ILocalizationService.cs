using System.Collections.ObjectModel;
using System.Globalization;

namespace Birds.UI.Services.Localization.Interfaces
{
    public interface ILocalizationService
    {
        ReadOnlyCollection<string> SupportedLanguages { get; }

        CultureInfo CurrentCulture { get; }

        string CurrentLanguage { get; }

        string this[string key] { get; }

        event EventHandler? LanguageChanged;

        string GetString(string key);

        string GetString(string key, params object[] args);

        bool ApplyLanguage(string? language);

        void LoadSavedLanguage();
    }
}
