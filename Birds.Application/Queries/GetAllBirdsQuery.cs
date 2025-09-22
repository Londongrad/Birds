using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Queries
{
    public record GetAllBirdsQuery() : IRequest<IReadOnlyList<BirdDTO>>;
}
