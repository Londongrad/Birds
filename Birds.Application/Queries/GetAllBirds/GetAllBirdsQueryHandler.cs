using AutoMapper;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Queries.GetAllBirds
{
    public class GetAllBirdsQueryHandler(IBirdRepository repository, IMapper mapper)
        : IRequestHandler<GetAllBirdsQuery, Result<IReadOnlyList<BirdDTO>>>
    {
        public async Task<Result<IReadOnlyList<BirdDTO>>> Handle(GetAllBirdsQuery query, CancellationToken cancellationToken = default)
        {
            if (query is null)
                return Result<IReadOnlyList<BirdDTO>>.Failure("Query cannot be null");

            var birds = await repository.GetAllAsync(cancellationToken);

            if (birds is null || birds.Count == 0)
                return Result<IReadOnlyList<BirdDTO>>.Failure("No birds found.");

            var mapped = mapper.Map<IReadOnlyList<BirdDTO>>(birds);

            return Result<IReadOnlyList<BirdDTO>>.Success(mapped);
        }
    }
}