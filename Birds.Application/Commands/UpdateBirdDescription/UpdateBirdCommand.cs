using Birds.Application.Common.Models;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdDescription
{
    public record UpdateBirdDescriptionCommand(
        Guid Id,
        string? Description
    ) : IRequest<Result>;
}