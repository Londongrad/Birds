using System.IO;
using Birds.UI.Services.Preferences.Interfaces;

namespace Birds.UI.Services.Preferences;

public sealed class LocalAppPreferencesPathProvider : IAppPreferencesPathProvider
{
    public string GetPreferencesPath()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Birds");

        return Path.Combine(directory, "preferences.json");
    }
}