using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Birds.Infrastructure.Repositories;
using Birds.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.Infrastructure
{
    public sealed class RemoteSyncServiceTests : IAsyncLifetime
    {
        private SqliteInMemoryDb _localDb = null!;
        private RemoteSqliteInMemoryDb _remoteDb = null!;

        public Task InitializeAsync()
        {
            _localDb = new SqliteInMemoryDb();
            _remoteDb = new RemoteSqliteInMemoryDb(ensureCreated: true);
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _localDb.DisposeAsync();
            await _remoteDb.DisposeAsync();
        }

        [Fact]
        public async Task SyncPendingAsync_WhenPendingUpsertExists_Should_UpsertRemoteBird_And_ClearOutbox()
        {
            var repository = new BirdRepository(_localDb.CreateFactory());
            var bird = Bird.Create(
                Enum.GetValues<BirdsName>()[0],
                "sync me",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)));
            await repository.AddAsync(bird);

            var sut = CreateSut(_remoteDb.CreateFactory());

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Synced);
            result.ProcessedCount.Should().Be(2);

            await using var remoteContext = _remoteDb.CreateContext();
            var remoteBird = await remoteContext.Birds.SingleAsync();
            remoteBird.Id.Should().Be(bird.Id);
            remoteBird.Description.Should().Be("sync me");

            await using var localContext = _localDb.CreateContext();
            (await localContext.SyncOperations.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task SyncPendingAsync_WhenPendingDeleteExists_Should_DeleteRemoteBird_And_ClearOutbox()
        {
            var repository = new BirdRepository(_localDb.CreateFactory());
            var bird = Bird.Create(
                Enum.GetValues<BirdsName>()[1],
                "remove remotely",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)));

            await using (var remoteContext = _remoteDb.CreateContext())
            {
                await remoteContext.Birds.AddAsync(Bird.Restore(
                    bird.Id,
                    bird.Name,
                    bird.Description,
                    bird.Arrival,
                    bird.Departure,
                    bird.IsAlive,
                    bird.CreatedAt,
                    bird.UpdatedAt));
                await remoteContext.SaveChangesAsync();
            }

            await repository.AddAsync(bird);
            await repository.RemoveAsync(bird);

            var sut = CreateSut(_remoteDb.CreateFactory());

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Synced);
            result.ProcessedCount.Should().Be(2);

            await using var remoteVerifyContext = _remoteDb.CreateContext();
            (await remoteVerifyContext.Birds.CountAsync()).Should().Be(0);
            var tombstone = await remoteVerifyContext.BirdTombstones.SingleAsync();
            tombstone.BirdId.Should().Be(bird.Id);
            tombstone.DeletedAtUtc.Kind.Should().Be(DateTimeKind.Unspecified);

            await using var localContext = _localDb.CreateContext();
            (await localContext.SyncOperations.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task SyncPendingAsync_WhenRemoteHasNewBird_Should_PullItIntoLocalStore_And_AdvanceCursor()
        {
            var remoteBird = Bird.Restore(
                Guid.NewGuid(),
                Enum.GetValues<BirdsName>()[2],
                "remote only",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)),
                null,
                true,
                DateTime.UtcNow.AddDays(-6),
                DateTime.UtcNow.AddDays(-2));

            await using (var remoteContext = _remoteDb.CreateContext())
            {
                await remoteContext.Birds.AddAsync(remoteBird);
                await remoteContext.SaveChangesAsync();
            }

            var sut = CreateSut(_remoteDb.CreateFactory());

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Synced);
            result.ProcessedCount.Should().Be(1);

            await using var localContext = _localDb.CreateContext();
            var localBird = await localContext.Birds.SingleAsync();
            localBird.Id.Should().Be(remoteBird.Id);
            localBird.Description.Should().Be("remote only");

            var cursor = await localContext.RemoteSyncCursors.SingleAsync();
            cursor.CursorKey.Should().Be("Birds.Pull");
            cursor.LastSyncedAtUtc.Should().Be(remoteBird.UpdatedAt);
        }

        [Fact]
        public async Task SyncPendingAsync_WhenRemoteBirdUpdatedAfterCursor_Should_UpdateLocalBird()
        {
            var birdId = Guid.NewGuid();
            var initialRemoteBird = Bird.Restore(
                birdId,
                Enum.GetValues<BirdsName>()[3],
                "before pull",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8)),
                null,
                true,
                DateTime.UtcNow.AddDays(-8),
                DateTime.UtcNow.AddDays(-5));

            await using (var remoteContext = _remoteDb.CreateContext())
            {
                await remoteContext.Birds.AddAsync(initialRemoteBird);
                await remoteContext.SaveChangesAsync();
            }

            var sut = CreateSut(_remoteDb.CreateFactory());
            await sut.SyncPendingAsync(CancellationToken.None);

            var updatedRemoteBird = Bird.Restore(
                birdId,
                initialRemoteBird.Name,
                "after pull",
                initialRemoteBird.Arrival,
                initialRemoteBird.Departure,
                initialRemoteBird.IsAlive,
                initialRemoteBird.CreatedAt,
                DateTime.UtcNow.AddDays(-1));

            await using (var remoteUpdateContext = _remoteDb.CreateContext())
            {
                remoteUpdateContext.Birds.Update(updatedRemoteBird);
                await remoteUpdateContext.SaveChangesAsync();
            }

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Synced);
            result.ProcessedCount.Should().Be(1);

            await using var localContext = _localDb.CreateContext();
            var localBird = await localContext.Birds.SingleAsync(bird => bird.Id == birdId);
            localBird.Description.Should().Be("after pull");
            localBird.UpdatedAt.Should().Be(updatedRemoteBird.UpdatedAt);
        }

        [Fact]
        public async Task SyncPendingAsync_WhenRemoteDeletionTombstoneExists_Should_DeleteLocalBird_And_AdvanceDeleteCursor()
        {
            var repository = new BirdRepository(_localDb.CreateFactory());
            var localBird = Bird.Create(
                Enum.GetValues<BirdsName>()[4],
                "delete locally",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)));
            await repository.AddAsync(localBird);

            var sut = CreateSut(_remoteDb.CreateFactory());
            await sut.SyncPendingAsync(CancellationToken.None);

            var deleteStamp = DateTime.UtcNow;
            await using (var remoteContext = _remoteDb.CreateContext())
            {
                remoteContext.Birds.RemoveRange(remoteContext.Birds.Where(bird => bird.Id == localBird.Id));
                await remoteContext.BirdTombstones.AddAsync(RemoteBirdTombstone.Create(localBird.Id, deleteStamp));
                await remoteContext.SaveChangesAsync();
            }

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Synced);
            result.ProcessedCount.Should().Be(1);

            await using var localContext = _localDb.CreateContext();
            (await localContext.Birds.CountAsync(bird => bird.Id == localBird.Id)).Should().Be(0);
            var cursor = await localContext.RemoteSyncCursors.SingleAsync(c => c.CursorKey == "Birds.Deletes.Pull");
            cursor.LastSyncedAtUtc.Should().Be(deleteStamp);
        }

        [Fact]
        public async Task SyncPendingAsync_WhenRemoteApplyFails_Should_KeepOutbox_And_MarkRetryState()
        {
            await using var brokenRemoteDb = new RemoteSqliteInMemoryDb(ensureCreated: false);
            var repository = new BirdRepository(_localDb.CreateFactory());
            var bird = Bird.Create(
                Enum.GetValues<BirdsName>()[1],
                "broken remote",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
            await repository.AddAsync(bird);

            var sut = CreateSut(brokenRemoteDb.CreateFactory());

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Failed);
            result.ProcessedCount.Should().Be(1);

            await using var localContext = _localDb.CreateContext();
            var operation = await localContext.SyncOperations.SingleAsync();
            operation.RetryCount.Should().Be(1);
            operation.LastAttemptAtUtc.Should().NotBeNull();
            operation.LastError.Should().NotBeNullOrWhiteSpace();
        }

        private RemoteSyncService CreateSut(IDbContextFactory<RemoteBirdDbContext> remoteFactory)
            => new(_localDb.CreateFactory(), remoteFactory, NullLogger<RemoteSyncService>.Instance);

        private sealed class RemoteSqliteInMemoryDb : IAsyncDisposable
        {
            private readonly SqliteConnection _connection;
            private readonly DbContextOptions<RemoteBirdDbContext> _options;

            public RemoteSqliteInMemoryDb(bool ensureCreated)
            {
                _connection = new SqliteConnection("Filename=:memory:");
                _connection.Open();
                _options = new DbContextOptionsBuilder<RemoteBirdDbContext>()
                    .UseSqlite(_connection)
                    .EnableSensitiveDataLogging()
                    .Options;

                if (ensureCreated)
                {
                    using var context = new RemoteBirdDbContext(_options);
                    context.Database.EnsureCreated();
                }
            }

            public RemoteBirdDbContext CreateContext() => new(_options);

            public IDbContextFactory<RemoteBirdDbContext> CreateFactory() => new TestRemoteBirdDbContextFactory(_options);

            public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

            private sealed class TestRemoteBirdDbContextFactory(DbContextOptions<RemoteBirdDbContext> options)
                : IDbContextFactory<RemoteBirdDbContext>
            {
                public RemoteBirdDbContext CreateDbContext() => new(options);

                public ValueTask<RemoteBirdDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(CreateDbContext());
            }
        }
    }
}
