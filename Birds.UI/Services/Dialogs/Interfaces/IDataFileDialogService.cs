namespace Birds.UI.Services.Dialogs.Interfaces;

public interface IDataFileDialogService
{
    string? PickImportPath(string suggestedPath);

    string? PickExportPath(string suggestedPath);
}