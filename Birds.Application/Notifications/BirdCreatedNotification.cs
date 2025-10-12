using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Notifications
{
    /// <summary>
    /// Notification published when a new bird is created.
    /// Used by the UI layer to update bird collections in real time.
    /// </summary>
    /// <param name="Bird">The data transfer object representing the newly created bird.</param>
    public record BirdCreatedNotification(BirdDTO Bird) : INotification;
}