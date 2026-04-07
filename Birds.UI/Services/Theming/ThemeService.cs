using Birds.UI.Services.Preferences;
using Birds.UI.Services.Theming.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;

namespace Birds.UI.Services.Theming
{
    public sealed class ThemeService : IThemeService
    {
        private const string PalettePrefix = "/Birds.UI;component/wwwroot/Themes/Palettes/";

        private static readonly IReadOnlyDictionary<string, string> ThemeSources = new Dictionary<string, string>
        {
            [AppPreferencesState.DefaultTheme] = $"{PalettePrefix}GraphiteTheme.xaml",
            ["Сталь"] = $"{PalettePrefix}SteelTheme.xaml"
        };

        public ThemeService()
        {
            AvailableThemes = new ReadOnlyCollection<string>(ThemeSources.Keys.ToList());
        }

        public ReadOnlyCollection<string> AvailableThemes { get; }

        public void ApplyTheme(string themeName)
        {
            if (System.Windows.Application.Current is null)
                return;

            if (!ThemeSources.TryGetValue(themeName, out var source))
                source = ThemeSources[AppPreferencesState.DefaultTheme];

            var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
            var existingIndex = dictionaries
                .Select((dictionary, index) => new { dictionary, index })
                .FirstOrDefault(item => item.dictionary.Source?.OriginalString.Contains(PalettePrefix, StringComparison.OrdinalIgnoreCase) == true)
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
}
