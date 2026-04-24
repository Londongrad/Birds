using System.IO;

namespace Birds.App.Services;

internal static class AppLogPathResolver
{
    private const string LogDirectoryEnvironmentVariable = "BIRDS_LOG_DIR";

    public static string ResolveLogsDirectory(string? startPath = null)
    {
        var configuredLogDirectory = Environment.GetEnvironmentVariable(LogDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredLogDirectory))
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredLogDirectory));

        var repositoryRoot = TryResolveRepositoryRoot(startPath);
        if (repositoryRoot is not null)
            return Path.Combine(repositoryRoot, "logs");

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Birds",
            "Logs");
    }

    private static string? TryResolveRepositoryRoot(string? startPath)
    {
        var current = ResolveStartDirectory(startPath);

        while (current is not null)
        {
            if (IsRepositoryRoot(current))
                return current.FullName;

            current = current.Parent;
        }

        return null;
    }

    private static DirectoryInfo ResolveStartDirectory(string? startPath)
    {
        var candidate = string.IsNullOrWhiteSpace(startPath)
            ? AppContext.BaseDirectory
            : startPath;

        var fullPath = Path.GetFullPath(candidate);
        var directoryPath = File.Exists(fullPath)
            ? Path.GetDirectoryName(fullPath)!
            : fullPath;

        return new DirectoryInfo(directoryPath);
    }

    private static bool IsRepositoryRoot(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateFiles("*.sln").Any()
                   || Directory.Exists(Path.Combine(directory.FullName, ".git"));
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
