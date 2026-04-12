using System.Globalization;

namespace Birds.Shared.Localization;

public static partial class AppText
{
    private static readonly IReadOnlyDictionary<string, string> Russian = BuildRussian();
    private static readonly IReadOnlyDictionary<string, string> English = BuildEnglish();

    public static string Get(string key, CultureInfo? culture = null)
    {
        var dictionary = SelectDictionary(culture);
        if (dictionary.TryGetValue(key, out var value))
            return value;

        return Russian.TryGetValue(key, out var fallback)
            ? fallback
            : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }

    public static string Format(CultureInfo? culture, string key, params object[] args)
    {
        var actualCulture = culture ?? CultureInfo.CurrentCulture;
        return string.Format(actualCulture, Get(key, actualCulture), args);
    }

    private static IReadOnlyDictionary<string, string> SelectDictionary(CultureInfo? culture)
    {
        var effectiveCulture = culture
                               ?? CultureInfo.DefaultThreadCurrentUICulture
                               ?? CultureInfo.CurrentUICulture;

        var language = AppLanguages.Normalize(effectiveCulture.Name);
        return language == AppLanguages.English ? English : Russian;
    }
}