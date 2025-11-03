using Birds.Application.Exceptions;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Repositories;
using FluentAssertions;

namespace Birds.Tests.Infrastructure
{
    public class BirdRepositoryTests : IAsyncLifetime
    {
        private SqliteInMemoryDb _db = null!;

        public Task InitializeAsync()
        {
            _db = new SqliteInMemoryDb();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await _db.DisposeAsync();

        [Fact]
        public async Task Add_Then_GetById_Should_Return_Same_Entity()
        {
            // Arrange
            var ctx = _db.CreateContext();
            var repo = new BirdRepository(ctx);

            var bird = Bird.Create(
                name: BirdsName.Воробей,
                description: "sparrow",
                arrival: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)));

            // Act
            await repo.AddAsync(bird);
            var found = await repo.GetByIdAsync(bird.Id);

            // Assert
            found.Id.Should().Be(bird.Id);
            found.Name.Should().Be(bird.Name);
        }

        [Fact]
        public async Task GetById_When_NotExists_Should_Throw_NotFound()
        {
            // Arrange
            var ctx = _db.CreateContext();
            var repo = new BirdRepository(ctx);

            // Act
            Func<Task> act = async () => await repo.GetByIdAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage("*Bird*not found*");
        }

        [Fact]
        public async Task GetAll_Should_Return_AsNoTracking_Entities()
        {
            // Arrange
            var ctx = _db.CreateContext();
            var repo = new BirdRepository(ctx);

            // Act
            var b1 = Bird.Create(BirdsName.Гайка, "tit", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
            var b2 = Bird.Create(BirdsName.Воробей, "sparrow", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)));
            await repo.AddAsync(b1);
            await repo.AddAsync(b2);

            // Assert
            var list = await repo.GetAllAsync();
            list.Should().HaveCount(2);

            // Check that entities are not tracked
            foreach (var e in list)
                ctx.Entry(e).State.Should().Be(Microsoft.EntityFrameworkCore.EntityState.Detached);
        }

        [Fact]
        public async Task Update_Should_Persist_Changes()
        {
            // Arrange
            var ctx = _db.CreateContext();
            var repo = new BirdRepository(ctx);

            // Act
            var bird = Bird.Create(BirdsName.Воробей, "old", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)));
            await repo.AddAsync(bird);

            bird.Update(BirdsName.Большак, "new", bird.Arrival, bird.Departure, true);
            await repo.UpdateAsync(bird);

            // Use new context to verify detached behavior
            using var ctx2 = _db.CreateContext();
            var repo2 = new BirdRepository(ctx2);
            var again = await repo2.GetByIdAsync(bird.Id);

            // Assert
            again.Name.Should().Be(BirdsName.Большак);
            again.Description.Should().Be("new");
        }

        [Fact]
        public async Task Remove_Should_Delete_Row()
        {
            // Arrange
            var ctx = _db.CreateContext();
            var repo = new BirdRepository(ctx);

            // Act
            var bird = Bird.Create(BirdsName.Воробей, "to delete", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
            await repo.AddAsync(bird);
            await repo.RemoveAsync(bird);
            Func<Task> act = async () => await repo.GetByIdAsync(bird.Id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}