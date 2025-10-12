using Birds.Application.Common.Models;
using Birds.Domain.Enums;
using MediatR;

namespace Birds.Application.Commands.UpdateBird
{
    public record UpdateBirdCommand(
        Guid Id,
        BirdsName Name, 
        string? Description, 
        DateOnly Arrival, 
        DateOnly? Departure, 
        bool IsAlive) 
        : IRequest<Result>;
}
