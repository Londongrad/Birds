using Birds.Application.Interfaces;
using Birds.Infrastructure.Persistence;

namespace Birds.Infrastructure.Repositories
{
    public class UnitOfWork(BirdDbContext context) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
    }
}
