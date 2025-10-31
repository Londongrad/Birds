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
        public async Task AddAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            await context.Birds.AddAsync(bird, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Birds
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.Birds
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Bird), id);
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            context.Birds.Remove(bird);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            context.Birds.Update(bird);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}