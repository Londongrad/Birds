using System.Collections.ObjectModel;
using System.Globalization;

namespace Birds.UI.Services.Localization.Interfaces;

public interface ILocalizationService
{
    ReadOnlyCollection<string> SupportedLanguages { get; }

    CultureInfo CurrentCulture { get; }

    string CurrentLanguage { get; }

    string CurrentDateFormat { get; }

    string this[string key] { get; }

    event EventHandler? LanguageChanged;

    string GetString(string key);

    string GetString(string key, params object[] args);

    bool ApplyLanguage(string? language);

    bool ApplyDateFormat(string? dateFormat);

    string FormatDate(DateOnly value, DateDisplayStyle style = DateDisplayStyle.Short);

    string FormatDate(DateOnly? value, DateDisplayStyle style = DateDisplayStyle.Short, string? fallback = null);

    string FormatDateTime(DateTime value);

    string FormatDateTime(DateTime? value, string? fallback = null);

    void LoadSavedLanguage();
}