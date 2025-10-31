using AutoMapper;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Shared.Constants;
using MediatR;

namespace Birds.Application.Queries.GetAllBirds
{
    public class GetAllBirdsQueryHandler(IBirdRepository repository, IMapper mapper)
        : IRequestHandler<GetAllBirdsQuery, Result<IReadOnlyList<BirdDTO>>>
    {
        public async Task<Result<IReadOnlyList<BirdDTO>>> Handle(GetAllBirdsQuery query, CancellationToken cancellationToken)
        {
            if (query is null)
                return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.QueryCannotBeNull);

            var birds = await repository.GetAllAsync(cancellationToken);

            if (birds is null || birds.Count == 0)
                return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.NoBirdsFound);

            var mapped = mapper.Map<IReadOnlyList<BirdDTO>>(birds);

            return Result<IReadOnlyList<BirdDTO>>.Success(mapped);
        }
    }
}