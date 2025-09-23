using Birds.Application.DTOs;
using MediatR;

namespace Birds.Application.Queries.GetAllBirds
{
    public record GetAllBirdsQuery() : IRequest<IReadOnlyList<BirdDTO>>;
}
