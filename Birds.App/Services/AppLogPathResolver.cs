using System.IO;

namespace Birds.App.Services
{
    internal static class AppLogPathResolver
    {
        public static string ResolveLogsDirectory(string? startPath = null)
        {
            var root = TryResolveRepositoryRoot(startPath)
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Birds");

            return Path.Combine(root, "logs");
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
}
