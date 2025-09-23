using Birds.Domain.Enums;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public record CreateBirdCommand(
        BirdsName Name,
        string? Description,
        DateOnly Arrival
    ) : IRequest<Guid>;
}
