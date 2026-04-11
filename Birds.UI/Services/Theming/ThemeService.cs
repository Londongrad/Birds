using System.Collections.ObjectModel;
using System.Windows;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Theming.Interfaces;

namespace Birds.UI.Services.Theming;

public sealed class ThemeService : IThemeService
{
    private const string PalettePrefix = "/Birds.UI;component/wwwroot/Themes/Palettes/";

    private static readonly IReadOnlyDictionary<string, string> ThemeSources = new Dictionary<string, string>
    {
        [ThemeKeys.Graphite] = $"{PalettePrefix}GraphiteTheme.xaml",
        [ThemeKeys.Steel] = $"{PalettePrefix}SteelTheme.xaml"
    };

    public ThemeService()
    {
        AvailableThemes = new ReadOnlyCollection<string>(ThemeKeys.SupportedThemes.ToList());
    }

    public ReadOnlyCollection<string> AvailableThemes { get; }

    public void ApplyTheme(string themeName)
    {
        if (System.Windows.Application.Current is null)
            return;

        themeName = ThemeKeys.Normalize(themeName);

        if (!ThemeSources.TryGetValue(themeName, out var source))
            source = ThemeSources[AppPreferencesState.DefaultTheme];

        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existingIndex = dictionaries
            .Select((dictionary, index) => new { dictionary, index })
            .FirstOrDefault(item =>
                item.dictionary.Source?.OriginalString.Contains(PalettePrefix, StringComparison.OrdinalIgnoreCase) ==
                true)
            ?.index;

        var newDictionary = new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        };

        if (existingIndex is int index)
            dictionaries[index] = newDictionary;
        else
            dictionaries.Insert(0, newDictionary);
    }
}