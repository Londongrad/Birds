using Birds.Application.Interfaces;
using Birds.Domain.Entities;

namespace Birds.Application.Queries
{
    public class GetBirdsQueryHandler(IBirdRepository repository)
    {
        private readonly IBirdRepository _repository = repository;

        public async Task<IReadOnlyList<Bird>> Handle(GetBirdsQuery query)
        {
            return await _repository.GetAllAsync();
        }
    }
}
