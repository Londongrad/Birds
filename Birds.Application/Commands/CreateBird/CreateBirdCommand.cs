using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public record CreateBirdCommand(
        string Name,
        string? Description,
        DateOnly Arrival
    ) : IRequest<Guid>;
}
