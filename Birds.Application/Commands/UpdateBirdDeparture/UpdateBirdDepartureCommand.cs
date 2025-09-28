using MediatR;

namespace Birds.Application.Commands.UpdateBirdDeparture
{
    public record UpdateBirdDepartureCommand(Guid Id, DateOnly? Departure) : IRequest;
}