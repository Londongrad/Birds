using System.Collections.ObjectModel;

namespace Birds.UI.Services.Theming.Interfaces
{
    public interface IThemeService
    {
        ReadOnlyCollection<string> AvailableThemes { get; }

        void ApplyTheme(string themeName);
    }
}
