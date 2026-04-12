namespace Birds.UI.Services.Shell.Interfaces;

public interface IPathNavigationService
{
    bool OpenDirectory(string path);
    bool OpenFile(string path);
}