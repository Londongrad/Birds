using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Seeding;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.Infrastructure
{
    public sealed class BirdSeederTests : IAsyncLifetime
    {
        private SqliteInMemoryDb _db = null!;

        public Task InitializeAsync()
        {
            _db = new SqliteInMemoryDb();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await _db.DisposeAsync();

        [Fact]
        public async Task SeedAsync_WhenSeedIfEmpty_Should_CreateRequestedNumberOfBirds()
        {
            // Arrange
            var seeder = new BirdSeeder(_db.CreateFactory(), NullLogger<BirdSeeder>.Instance);
            var options = new DatabaseSeedingOptions(
                DatabaseSeedingMode.SeedIfEmpty,
                recordCount: 250,
                batchSize: 40,
                randomSeed: 42);

            // Act
            await seeder.SeedAsync(options, CancellationToken.None);

            // Assert
            await using var context = _db.CreateContext();
            var birds = await context.Birds.AsNoTracking().ToListAsync();

            birds.Should().HaveCount(250);
            birds.Should().OnlyContain(x => x.IsAlive || x.Departure != null);
            birds.Should().Contain(x => x.Description == null);
            birds.Should().Contain(x => x.Departure == null);
        }

        [Fact]
        public async Task SeedAsync_WhenSeedIfEmptyAndDataExists_Should_NotDuplicateData()
        {
            // Arrange
            await using (var context = _db.CreateContext())
            {
                context.Birds.Add(Bird.Create(
                    BirdsName.Воробей,
                    "existing",
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-5))));

                await context.SaveChangesAsync();
            }

            var seeder = new BirdSeeder(_db.CreateFactory(), NullLogger<BirdSeeder>.Instance);
            var options = new DatabaseSeedingOptions(
                DatabaseSeedingMode.SeedIfEmpty,
                recordCount: 100,
                batchSize: 25,
                randomSeed: 7);

            // Act
            await seeder.SeedAsync(options, CancellationToken.None);

            // Assert
            await using var verificationContext = _db.CreateContext();
            (await verificationContext.Birds.CountAsync()).Should().Be(1);
        }
    }
}
