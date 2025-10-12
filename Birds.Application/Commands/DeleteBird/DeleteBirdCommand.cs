using Birds.Application.Common.Models;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public record DeleteBirdCommand(Guid Id) : IRequest<Result>;
}