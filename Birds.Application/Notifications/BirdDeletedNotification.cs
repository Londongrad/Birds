using MediatR;

namespace Birds.Application.Notifications
{
    public record BirdDeletedNotification(Guid BirdId) : INotification;
}
