using System.Text.Json;
using Birds.Application.Exceptions;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence.Models;
using Birds.Infrastructure.Repositories;
using Birds.Shared.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Birds.Tests.Infrastructure;

public class BirdRepositoryTests : IAsyncLifetime
{
    private SqliteInMemoryDb _db = null!;

    public Task InitializeAsync()
    {
        _db = new SqliteInMemoryDb();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task Add_Then_GetById_Should_Return_Same_Entity()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var species = Enum.GetValues<BirdSpecies>()[0];

        var bird = Bird.Create(
            species,
            "sparrow",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));

        await repo.AddAsync(bird);
        var found = await repo.GetByIdAsync(bird.Id);

        found.Id.Should().Be(bird.Id);
        found.Name.Should().Be(bird.Name);
    }

    [Fact]
    public async Task GetById_Should_Read_Legacy_Russian_Enum_Name_From_Database()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var bird = Bird.Create(
            BirdSpecies.Sparrow,
            "legacy row",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));

        await repo.AddAsync(bird);

        await using (var context = _db.CreateContext())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "Birds" SET "Name" = {"Воробей"} WHERE "Id" = {bird.Id}""");
        }

        var found = await repo.GetByIdAsync(bird.Id);

        found.Name.Should().Be(BirdSpecies.Sparrow);
    }

    [Fact]
    public async Task GetById_When_NotExists_Should_Throw_NotFound()
    {
        var repo = new BirdRepository(_db.CreateFactory());

        Func<Task> act = async () => await repo.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(ExceptionMessages.NotFound("Bird", "*"));
    }

    [Fact]
    public async Task GetAll_Should_Return_AsNoTracking_Entities()
    {
        var ctx = _db.CreateContext();
        var repo = new BirdRepository(_db.CreateFactory());
        var firstSpecies = Enum.GetValues<BirdSpecies>()[0];
        var secondSpecies = Enum.GetValues<BirdSpecies>()[1];

        var b1 = Bird.Create(firstSpecies, "tit", DateOnly.FromDateTime(DateTime.Now.AddDays(-5)));
        var b2 = Bird.Create(secondSpecies, "sparrow", DateOnly.FromDateTime(DateTime.Now.AddDays(-4)));
        await repo.AddAsync(b1);
        await repo.AddAsync(b2);

        var list = await repo.GetAllAsync();
        list.Should().HaveCount(2);

        foreach (var e in list)
            ctx.Entry(e).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task Update_Should_Persist_Changes()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var originalSpecies = Enum.GetValues<BirdSpecies>()[0];
        var updatedSpecies = Enum.GetValues<BirdSpecies>()[2];

        var bird = Bird.Create(originalSpecies, "old", DateOnly.FromDateTime(DateTime.Now.AddDays(-10)));
        await repo.AddAsync(bird);
        var createdAt = bird.CreatedAt;

        await repo.UpdateAsync(
            bird.Id,
            bird.Version,
            updatedSpecies,
            "new",
            bird.Arrival,
            bird.Departure,
            true);

        var repo2 = new BirdRepository(_db.CreateFactory());
        var again = await repo2.GetByIdAsync(bird.Id);

        again.Name.Should().Be(updatedSpecies);
        again.Description.Should().Be("new");
        again.CreatedAt.Should().Be(createdAt);
        again.Version.Should().Be(2);
    }

    [Fact]
    public async Task Add_Should_Start_With_Initial_Version()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var bird = Bird.Create(
            BirdSpecies.Sparrow,
            "versioned",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-10)));

        await repo.AddAsync(bird);

        var stored = await repo.GetByIdAsync(bird.Id);
        stored.Version.Should().Be(Bird.InitialVersion);
    }

    [Fact]
    public async Task Update_With_Stale_Version_Should_Not_Overwrite_Newer_Data()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var bird = Bird.Create(
            BirdSpecies.Sparrow,
            "original",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-10)));
        await repo.AddAsync(bird);
        var staleVersion = bird.Version;

        await repo.UpdateAsync(
            bird.Id,
            staleVersion,
            BirdSpecies.Goldfinch,
            "newer value",
            bird.Arrival,
            bird.Departure,
            bird.IsAlive);

        Func<Task> act = async () => await repo.UpdateAsync(
            bird.Id,
            staleVersion,
            BirdSpecies.Amadin,
            "stale value",
            bird.Arrival,
            bird.Departure,
            bird.IsAlive);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        var stored = await repo.GetByIdAsync(bird.Id);
        stored.Name.Should().Be(BirdSpecies.Goldfinch);
        stored.Description.Should().Be("newer value");
        stored.Version.Should().Be(2);
    }

    [Fact]
    public async Task Ef_Should_Reject_Concurrent_Bird_Update_When_Version_Is_Stale()
    {
        var bird = Bird.Create(
            BirdSpecies.Sparrow,
            "original",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-10)));

        await using (var seedContext = _db.CreateContext())
        {
            await seedContext.Birds.AddAsync(bird);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = _db.CreateContext();
        await using var secondContext = _db.CreateContext();
        var firstCopy = await firstContext.Birds.SingleAsync(candidate => candidate.Id == bird.Id);
        var staleCopy = await secondContext.Birds.SingleAsync(candidate => candidate.Id == bird.Id);

        firstCopy.Update(BirdSpecies.Goldfinch, "newer", firstCopy.Arrival, firstCopy.Departure, firstCopy.IsAlive);
        await firstContext.SaveChangesAsync();

        staleCopy.Update(BirdSpecies.Amadin, "stale", staleCopy.Arrival, staleCopy.Departure, staleCopy.IsAlive);
        Func<Task> act = async () => await secondContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verifyContext = _db.CreateContext();
        var stored = await verifyContext.Birds.SingleAsync(candidate => candidate.Id == bird.Id);
        stored.Description.Should().Be("newer");
        stored.Version.Should().Be(2);
    }

    [Fact]
    public async Task Remove_Should_Delete_Row()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var species = Enum.GetValues<BirdSpecies>()[0];

        var bird = Bird.Create(species, "to delete", DateOnly.FromDateTime(DateTime.Now.AddDays(-2)));
        await repo.AddAsync(bird);
        await repo.RemoveAsync(bird);
        Func<Task> act = async () => await repo.GetByIdAsync(bird.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Upsert_Should_Add_And_Update_Birds_In_One_Pass()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var primarySpecies = Enum.GetValues<BirdSpecies>()[0];
        var secondarySpecies = Enum.GetValues<BirdSpecies>()[1];

        var existing = Bird.Create(primarySpecies, "old", DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));
        await repo.AddAsync(existing);

        var updated = Bird.Restore(
            existing.Id,
            primarySpecies,
            "updated",
            existing.Arrival,
            existing.Departure,
            existing.IsAlive,
            existing.CreatedAt,
            existing.UpdatedAt);

        var added = Bird.Create(secondarySpecies, "new", DateOnly.FromDateTime(DateTime.Now.AddDays(-1)));

        var result = await repo.UpsertAsync(new[] { updated, added });
        var list = await repo.GetAllAsync();

        result.Added.Should().Be(1);
        result.Updated.Should().Be(1);
        list.Should().HaveCount(2);
        list.Should().Contain(x => x.Id == existing.Id && x.Description == "updated");
        list.Single(x => x.Id == existing.Id).Version.Should().Be(2);
        list.Should().Contain(x => x.Id == added.Id);
    }

    [Fact]
    public async Task ReplaceWithSnapshot_Should_Remove_Missing_Birds_And_Upsert_Current_Ones()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var species = Enum.GetValues<BirdSpecies>();

        var retained = Bird.Create(species[0], "retain", DateOnly.FromDateTime(DateTime.Now.AddDays(-4)));
        var removed = Bird.Create(species[1], "remove", DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));
        await repo.AddAsync(retained);
        await repo.AddAsync(removed);

        var updatedRetained = Bird.Restore(
            retained.Id,
            retained.Name,
            "updated retain",
            retained.Arrival,
            retained.Departure,
            retained.IsAlive,
            retained.CreatedAt,
            retained.UpdatedAt);
        var added = Bird.Create(species[2], "added", DateOnly.FromDateTime(DateTime.Now.AddDays(-1)));

        var result = await repo.ReplaceWithSnapshotAsync(new[] { updatedRetained, added });
        var list = await repo.GetAllAsync();

        result.Added.Should().Be(1);
        result.Updated.Should().Be(1);
        result.Removed.Should().Be(1);
        list.Should().HaveCount(2);
        list.Should().Contain(x => x.Id == retained.Id && x.Description == "updated retain");
        list.Should().Contain(x => x.Id == added.Id);
        list.Should().NotContain(x => x.Id == removed.Id);
    }

    [Fact]
    public async Task Add_Should_Queue_Pending_Upsert_Sync_Operation()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var species = Enum.GetValues<BirdSpecies>()[0];
        var bird = Bird.Create(species, "queued", DateOnly.FromDateTime(DateTime.Now.AddDays(-2)));
        var beforeUtc = DateTime.UtcNow;

        await repo.AddAsync(bird);
        var afterUtc = DateTime.UtcNow;

        await using var context = _db.CreateContext();
        var operation = await context.SyncOperations.SingleAsync();

        operation.AggregateId.Should().Be(bird.Id);
        operation.OperationType.Should().Be(SyncOperationType.Upsert);
        operation.CreatedAtUtc.Should().BeOnOrAfter(beforeUtc).And.BeOnOrBefore(afterUtc);
        operation.UpdatedAtUtc.Should().BeOnOrAfter(beforeUtc).And.BeOnOrBefore(afterUtc);
        operation.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        operation.UpdatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);

        using var payload = JsonDocument.Parse(operation.PayloadJson);
        payload.RootElement.GetProperty("Id").GetGuid().Should().Be(bird.Id);
        payload.RootElement.GetProperty("Name").GetString().Should().Be(bird.Name.ToString());
        payload.RootElement.GetProperty("Species").GetString().Should().Be(bird.Name.ToString());
        payload.RootElement.GetProperty("SyncStampUtc").GetDateTime().Should().Be(bird.SyncStampUtc);
    }

    [Fact]
    public async Task Update_Should_Coalesce_To_A_Single_Pending_Upsert_Operation()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var species = Enum.GetValues<BirdSpecies>();
        var bird = Bird.Create(species[0], "before", DateOnly.FromDateTime(DateTime.Now.AddDays(-5)));
        await repo.AddAsync(bird);

        Guid originalOperationId;
        await using (var initialContext = _db.CreateContext())
        {
            originalOperationId = await initialContext.SyncOperations
                .Select(operation => operation.Id)
                .SingleAsync();
        }

        await repo.UpdateAsync(
            bird.Id,
            bird.Version,
            species[1],
            "after",
            bird.Arrival,
            bird.Departure,
            bird.IsAlive);

        await using var context = _db.CreateContext();
        var operations = await context.SyncOperations.ToListAsync();

        operations.Should().HaveCount(1);
        operations[0].Id.Should().Be(originalOperationId);
        operations[0].OperationType.Should().Be(SyncOperationType.Upsert);

        using var payload = JsonDocument.Parse(operations[0].PayloadJson);
        payload.RootElement.GetProperty("Description").GetString().Should().Be("after");
        payload.RootElement.GetProperty("Name").GetString().Should().Be(species[1].ToString());
    }

    [Fact]
    public async Task Remove_Should_Replace_Pending_Upsert_With_Delete_Operation()
    {
        var repo = new BirdRepository(_db.CreateFactory());
        var species = Enum.GetValues<BirdSpecies>()[0];
        var bird = Bird.Create(species, "delete me", DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));
        await repo.AddAsync(bird);

        Guid originalOperationId;
        await using (var initialContext = _db.CreateContext())
        {
            originalOperationId = await initialContext.SyncOperations
                .Select(operation => operation.Id)
                .SingleAsync();
        }

        await repo.RemoveAsync(bird);

        await using var context = _db.CreateContext();
        var operations = await context.SyncOperations.ToListAsync();

        operations.Should().HaveCount(1);
        operations[0].Id.Should().Be(originalOperationId);
        operations[0].OperationType.Should().Be(SyncOperationType.Delete);

        using var payload = JsonDocument.Parse(operations[0].PayloadJson);
        payload.RootElement.GetProperty("Id").GetGuid().Should().Be(bird.Id);
    }
}
