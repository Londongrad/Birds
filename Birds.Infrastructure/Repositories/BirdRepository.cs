using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace Birds.Infrastructure.Repositories
{
    public class BirdRepository(IDbContextFactory<BirdDbContext> contextFactory) : IBirdRepository
    {
        /// <inheritdoc/>
        public async Task AddAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Birds.AddAsync(bird, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var birds = await context.Birds.AsNoTracking().ToListAsync(cancellationToken);
            return birds.ToImmutableArray();
        }

        /// <inheritdoc/>
        public async Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Birds.FindAsync([id], cancellationToken)
                ?? throw new NotFoundException(nameof(Bird), id);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Birds.Where(b => b.Id == id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Birds.Where(b => b.Id == bird.Id)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(p => p.Name, bird.Name)
                    .SetProperty(p => p.Description, bird.Description)
                    .SetProperty(p => p.Arrival, bird.Arrival)
                    .SetProperty(p => p.Departure, bird.Departure)
                    .SetProperty(p => p.IsAlive, bird.IsAlive),
                cancellationToken);
        }
    }
}