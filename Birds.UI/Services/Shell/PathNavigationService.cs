using System.Diagnostics;
using System.IO;
using Birds.UI.Services.Shell.Interfaces;

namespace Birds.UI.Services.Shell;

public sealed class PathNavigationService : IPathNavigationService
{
    public bool OpenDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(fullPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{fullPath}\"",
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
