using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public record CreateBirdCommand(
        BirdsName Name,
        string? Description,
        DateOnly Arrival,
        DateOnly? Departure = null,
        bool IsAlive = true
    ) : IRequest<Result<BirdDTO>>;
}