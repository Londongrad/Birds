using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Notifications
{
    /// <summary>
    /// Notification published when an existing bird is updated.
    /// Used by the UI layer to refresh the displayed bird information in its collection.
    /// </summary>
    /// <param name="BirdDTO">The updated bird data transfer object.</param>
    public record BirdUpdatedNotification(BirdDTO BirdDTO) : INotification;
}
