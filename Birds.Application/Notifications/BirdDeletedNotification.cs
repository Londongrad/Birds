using MediatR;

namespace Birds.Application.Notifications
{
    /// <summary>
    /// Notification published when an existing bird is deleted.
    /// Used by the UI layer to remove the corresponding bird from its collection.
    /// </summary>
    /// <param name="BirdId">The unique identifier of the deleted bird.</param>
    public record BirdDeletedNotification(Guid BirdId) : INotification;
}
