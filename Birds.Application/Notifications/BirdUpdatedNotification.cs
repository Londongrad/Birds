using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Notifications
{
    public record BirdUpdatedNotification(BirdDTO BirdDTO) : INotification;
}
