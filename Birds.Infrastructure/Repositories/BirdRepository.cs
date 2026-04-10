using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Application.Common.Models;
using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Repositories
{
    public class BirdRepository(IDbContextFactory<BirdDbContext> contextFactory) : IBirdRepository
    {
        /// <inheritdoc/>
        public async Task AddAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Birds.AddAsync(bird, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Birds
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Birds
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Bird), id);
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            context.Birds.Remove(bird);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Bird bird, CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            context.Birds.Update(bird);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<UpsertBirdsResult> UpsertAsync(
            IReadOnlyCollection<Bird> birds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(birds);

            if (birds.Count == 0)
                return new UpsertBirdsResult(0, 0);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var ids = birds
                .Select(static bird => bird.Id)
                .Distinct()
                .ToArray();

            var existingIds = await context.Birds
                .AsNoTracking()
                .Where(bird => ids.Contains(bird.Id))
                .Select(static bird => bird.Id)
                .ToListAsync(cancellationToken);

            var existingSet = existingIds.ToHashSet();
            var toAdd = birds.Where(bird => !existingSet.Contains(bird.Id)).ToList();
            var toUpdate = birds.Where(bird => existingSet.Contains(bird.Id)).ToList();

            if (toAdd.Count > 0)
                await context.Birds.AddRangeAsync(toAdd, cancellationToken);

            if (toUpdate.Count > 0)
                context.Birds.UpdateRange(toUpdate);

            await context.SaveChangesAsync(cancellationToken);

            return new UpsertBirdsResult(toAdd.Count, toUpdate.Count);
        }
    }
}
