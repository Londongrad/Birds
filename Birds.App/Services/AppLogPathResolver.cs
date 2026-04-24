using System.IO;

namespace Birds.App.Services;

internal static class AppLogPathResolver
{
    public static string ResolveLogsDirectory(string? startPath = null)
    {
        _ = startPath;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Birds",
            "Logs");
    }
}
