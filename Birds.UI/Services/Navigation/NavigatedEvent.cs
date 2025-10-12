using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Windows;

namespace Birds.UI.Services.Navigation
{
    /// <summary>
    /// Navigation event published via MediatR after a new window is opened.
    /// <para>
    /// Contains a reference to the <see cref="Window"/> that was opened
    /// and the corresponding <see cref="ObservableObject"/> ViewModel assigned to that window.
    /// </para>
    /// Other services (e.g., a notification service) can handle this event
    /// to attach to the newly opened window or perform additional logic
    /// related to context switching.
    /// </summary>
    /// <param name="Window">The instance of the window that was opened.</param>
    /// <param name="ViewModel">The ViewModel that became active for the specified window.</param>
    public record NavigatedEvent(Window Window, ObservableObject ViewModel) : INotification;
}
