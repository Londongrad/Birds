using MediatR;

namespace Birds.Application.Commands.UpdateBirdStatus
{
    public record UpdateBirdStatusCommand(Guid Id, bool IsAlive) : IRequest;
}