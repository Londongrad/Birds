using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Repositories
{
    public class BirdRepository(BirdDbContext context) : IBirdRepository
    {
        /// <inheritdoc/>
        public async Task<Guid> AddAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            await context.Birds.AddAsync(bird, cancellationToken);
            return bird.Id;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Birds.AsNoTracking().ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.Birds.FindAsync([id], cancellationToken)
                ?? throw new NotFoundException(nameof(Bird), id);
        }

        /// <inheritdoc/>
        public void Remove(Bird bird)
        {
            context.Birds.Remove(bird);
        }

        /// <inheritdoc/>
        public void Update(Bird bird)
        {
            context.Birds.Update(bird);
        }
    }
}
