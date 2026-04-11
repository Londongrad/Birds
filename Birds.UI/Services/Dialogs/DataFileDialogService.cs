using System.IO;
using Birds.UI.Services.Dialogs.Interfaces;
using Microsoft.Win32;

namespace Birds.UI.Services.Dialogs;

public sealed class DataFileDialogService : IDataFileDialogService
{
    private const string JsonFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

    public string? PickImportPath(string suggestedPath)
    {
        var dialog = new OpenFileDialog
        {
            Filter = JsonFilter,
            Multiselect = false,
            CheckFileExists = true,
            DefaultExt = ".json",
            InitialDirectory = ResolveDirectory(suggestedPath),
            FileName = ResolveFileName(suggestedPath)
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickExportPath(string suggestedPath)
    {
        var dialog = new SaveFileDialog
        {
            Filter = JsonFilter,
            DefaultExt = ".json",
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = ResolveDirectory(suggestedPath),
            FileName = ResolveFileName(suggestedPath)
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string ResolveDirectory(string suggestedPath)
    {
        var directory = Path.GetDirectoryName(suggestedPath);
        return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)
            ? directory
            : Environment.CurrentDirectory;
    }

    private static string ResolveFileName(string suggestedPath)
    {
        var fileName = Path.GetFileName(suggestedPath);
        return string.IsNullOrWhiteSpace(fileName) ? "birds.json" : fileName;
    }
}