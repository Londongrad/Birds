using System.Windows;

namespace Birds.UI.Services.Interfaces
{
    public interface INotificationService
    {
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowInfo(string message);
        void ShowWarning(string message);
    }
}
