using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Notifications
{
    public record BirdCreatedNotification(BirdDTO Bird) : INotification;
}
