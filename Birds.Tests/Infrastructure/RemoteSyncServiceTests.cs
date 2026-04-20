using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Birds.Infrastructure.Persistence.Models;
using Birds.Infrastructure.Repositories;
using Birds.Infrastructure.Services;
using Birds.Shared.Sync;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Birds.Tests.Infrastructure;

public sealed class RemoteSyncServiceTests : IAsyncLifetime
{
    private SqliteInMemoryDb _localDb = null!;
    private RemoteSqliteInMemoryDb _remoteDb = null!;

    public Task InitializeAsync()
    {
        _localDb = new SqliteInMemoryDb();
        _remoteDb = new RemoteSqliteInMemoryDb(true);
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
            DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));
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
            DateOnly.FromDateTime(DateTime.Now.AddDays(-4)));

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
    public async Task
        SyncPendingAsync_WhenRemoteBirdIsNewerThanPendingUpsert_Should_KeepRemoteVersion_And_ReconcileLocal()
    {
        var repository = new BirdRepository(_localDb.CreateFactory());
        var localBird = Bird.Create(
            Enum.GetValues<BirdsName>()[0],
            "local version",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-3)));
        await repository.AddAsync(localBird);

        var newerRemoteBird = Bird.Restore(
            localBird.Id,
            localBird.Name,
            "remote version",
            localBird.Arrival,
            localBird.Departure,
            localBird.IsAlive,
            localBird.CreatedAt,
            DateTime.Now.AddMinutes(5));

        await using (var remoteContext = _remoteDb.CreateContext())
        {
            await remoteContext.Birds.AddAsync(newerRemoteBird);
            await remoteContext.SaveChangesAsync();
        }

        var sut = CreateSut(_remoteDb.CreateFactory());

        var result = await sut.SyncPendingAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Synced);
        result.RemoteWinsCount.Should().Be(1);

        await using (var remoteVerifyContext = _remoteDb.CreateContext())
        {
            var remoteBird = await remoteVerifyContext.Birds.SingleAsync(bird => bird.Id == localBird.Id);
            remoteBird.Description.Should().Be("remote version");
            remoteBird.UpdatedAt.Should().Be(newerRemoteBird.UpdatedAt);
        }

        await using (var localVerifyContext = _localDb.CreateContext())
        {
            var reconciledLocalBird = await localVerifyContext.Birds.SingleAsync(bird => bird.Id == localBird.Id);
            reconciledLocalBird.Description.Should().Be("remote version");
            reconciledLocalBird.UpdatedAt.Should().Be(newerRemoteBird.UpdatedAt);
            (await localVerifyContext.SyncOperations.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task
        SyncPendingAsync_WhenRemoteBirdIsNewerThanPendingDelete_Should_RestoreLocalBird_And_KeepRemoteRecord()
    {
        var repository = new BirdRepository(_localDb.CreateFactory());
        var localBird = Bird.Create(
            Enum.GetValues<BirdsName>()[1],
            "delete me locally",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-4)));
        await repository.AddAsync(localBird);
        await repository.RemoveAsync(localBird);

        var newerRemoteBird = Bird.Restore(
            localBird.Id,
            localBird.Name,
            "remote wins",
            localBird.Arrival,
            localBird.Departure,
            localBird.IsAlive,
            localBird.CreatedAt,
            DateTime.Now.AddMinutes(5));

        await using (var remoteContext = _remoteDb.CreateContext())
        {
            await remoteContext.Birds.AddAsync(newerRemoteBird);
            await remoteContext.SaveChangesAsync();
        }

        var sut = CreateSut(_remoteDb.CreateFactory());

        var result = await sut.SyncPendingAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Synced);
        result.RemoteWinsCount.Should().Be(1);

        await using (var remoteVerifyContext = _remoteDb.CreateContext())
        {
            var remoteBird = await remoteVerifyContext.Birds.SingleAsync(bird => bird.Id == localBird.Id);
            remoteBird.Description.Should().Be("remote wins");
        }

        await using (var localVerifyContext = _localDb.CreateContext())
        {
            var restoredLocalBird = await localVerifyContext.Birds.SingleAsync(bird => bird.Id == localBird.Id);
            restoredLocalBird.Description.Should().Be("remote wins");
            restoredLocalBird.UpdatedAt.Should().Be(newerRemoteBird.UpdatedAt);
            (await localVerifyContext.SyncOperations.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task SyncPendingAsync_WhenRemoteHasNewBird_Should_PullItIntoLocalStore_And_AdvanceCursor()
    {
        var remoteBird = Bird.Restore(
            Guid.NewGuid(),
            Enum.GetValues<BirdsName>()[2],
            "remote only",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-6)),
            null,
            true,
            DateTime.Now.AddDays(-6),
            DateTime.Now.AddDays(-2));

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
            DateOnly.FromDateTime(DateTime.Now.AddDays(-8)),
            null,
            true,
            DateTime.Now.AddDays(-8),
            DateTime.Now.AddDays(-5));

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
            DateTime.Now.AddDays(-1));

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
    public async Task SyncPendingAsync_WhenLocalBirdIsNewerThanRemotePullCandidate_Should_KeepLocalVersion()
    {
        var birdId = Guid.NewGuid();
        var remoteBird = Bird.Restore(
            birdId,
            Enum.GetValues<BirdsName>()[2],
            "remote stale",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-8)),
            null,
            true,
            DateTime.Now.AddDays(-8),
            DateTime.Now.AddDays(-3));

        await using (var remoteContext = _remoteDb.CreateContext())
        {
            await remoteContext.Birds.AddAsync(remoteBird);
            await remoteContext.SaveChangesAsync();
        }

        await using (var localContext = _localDb.CreateContext())
        {
            await localContext.Birds.AddAsync(Bird.Restore(
                remoteBird.Id,
                remoteBird.Name,
                "local newer",
                remoteBird.Arrival,
                remoteBird.Departure,
                remoteBird.IsAlive,
                remoteBird.CreatedAt,
                DateTime.Now.AddMinutes(10)));
            await localContext.SaveChangesAsync();
        }

        var sut = CreateSut(_remoteDb.CreateFactory());

        var result = await sut.SyncPendingAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Synced);
        result.RemoteWinsCount.Should().Be(0);

        await using var localVerifyContext = _localDb.CreateContext();
        var localBird = await localVerifyContext.Birds.SingleAsync(bird => bird.Id == birdId);
        localBird.Description.Should().Be("local newer");
    }

    [Fact]
    public async Task
        SyncPendingAsync_WhenRemoteDeletionTombstoneExists_Should_DeleteLocalBird_And_AdvanceDeleteCursor()
    {
        var repository = new BirdRepository(_localDb.CreateFactory());
        var localBird = Bird.Create(
            Enum.GetValues<BirdsName>()[4],
            "delete locally",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-7)));
        await repository.AddAsync(localBird);

        var sut = CreateSut(_remoteDb.CreateFactory());
        await sut.SyncPendingAsync(CancellationToken.None);

        var deleteStamp = DateTime.Now;
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
    public async Task SyncPendingAsync_WhenLocalBirdIsNewerThanRemoteDeleteTombstone_Should_KeepLocalBird()
    {
        var localBird = Bird.Restore(
            Guid.NewGuid(),
            Enum.GetValues<BirdsName>()[4],
            "local survives",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-7)),
            null,
            true,
            DateTime.Now.AddDays(-7),
            DateTime.Now.AddMinutes(12));

        await using (var localContext = _localDb.CreateContext())
        {
            await localContext.Birds.AddAsync(localBird);
            await localContext.SaveChangesAsync();
        }

        await using (var remoteContext = _remoteDb.CreateContext())
        {
            await remoteContext.BirdTombstones.AddAsync(
                RemoteBirdTombstone.Create(localBird.Id, DateTime.Now.AddMinutes(-2)));
            await remoteContext.SaveChangesAsync();
        }

        var sut = CreateSut(_remoteDb.CreateFactory());

        var result = await sut.SyncPendingAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Synced);
        result.RemoteWinsCount.Should().Be(0);

        await using var localVerifyContext = _localDb.CreateContext();
        (await localVerifyContext.Birds.CountAsync(bird => bird.Id == localBird.Id)).Should().Be(1);
    }

    [Fact]
    public async Task SyncPendingAsync_WhenRemoteApplyFails_Should_KeepOutbox_And_MarkRetryState()
    {
        var repository = new BirdRepository(_localDb.CreateFactory());
        var bird = Bird.Create(
            Enum.GetValues<BirdsName>()[1],
            "broken remote",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-2)));
        await repository.AddAsync(bird);

        var remoteFactory = new Mock<IDbContextFactory<RemoteBirdDbContext>>();
        remoteFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broken remote"));

        var sut = new RemoteSyncService(
            _localDb.CreateFactory(),
            remoteFactory.Object,
            NullLogger<RemoteSyncService>.Instance);

        var result = await sut.SyncPendingAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Failed);
        result.ProcessedCount.Should().Be(1);

        await using var localContext = _localDb.CreateContext();
        var operation = await localContext.SyncOperations.SingleAsync();
        operation.RetryCount.Should().Be(1);
        operation.LastAttemptAtUtc.Should().NotBeNull();
        operation.LastError.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CheckBackendAvailabilityAsync_Should_ReportEmptyRemoteSnapshot_WhenNoBirdsExist()
    {
        await using var emptyRemoteDb = new RemoteSqliteInMemoryDb(false);
        var sut = CreateSut(emptyRemoteDb.CreateFactory());

        var result = await sut.CheckBackendAvailabilityAsync(CancellationToken.None);

        result.IsReady.Should().BeTrue();
        result.RemoteBirdCount.Should().Be(0);
        result.RemoteSnapshotState.Should().Be(RemoteSyncSnapshotState.Empty);
    }

    [Fact]
    public async Task UploadLocalSnapshotAsync_Should_ReplaceRemoteBirds_And_ClearLocalSyncState()
    {
        var repository = new BirdRepository(_localDb.CreateFactory());
        var firstBird = Bird.Create(
            Enum.GetValues<BirdsName>()[0],
            "first local",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-4)));
        var secondBird = Bird.Create(
            Enum.GetValues<BirdsName>()[1],
            "second local",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-2)));

        await repository.AddAsync(firstBird);
        await repository.AddAsync(secondBird);

        await using (var remoteContext = _remoteDb.CreateContext())
        {
            await remoteContext.Birds.AddAsync(Bird.Restore(
                Guid.NewGuid(),
                Enum.GetValues<BirdsName>()[2],
                "stale remote",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-10)),
                null,
                true,
                DateTime.Now.AddDays(-10),
                DateTime.Now.AddDays(-8)));
            await remoteContext.BirdTombstones.AddAsync(RemoteBirdTombstone.Create(Guid.NewGuid(), DateTime.UtcNow));
            await remoteContext.SaveChangesAsync();
        }

        var sut = CreateSut(_remoteDb.CreateFactory());

        var result = await sut.UploadLocalSnapshotAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Synced);
        result.ProcessedCount.Should().Be(2);

        await using (var remoteVerifyContext = _remoteDb.CreateContext())
        {
            var remoteBirds = await remoteVerifyContext.Birds
                .OrderBy(bird => bird.Description)
                .ToListAsync();
            remoteBirds.Should().HaveCount(2);
            remoteBirds.Select(bird => bird.Description).Should().Contain(["first local", "second local"]);
            (await remoteVerifyContext.BirdTombstones.CountAsync()).Should().Be(0);
        }

        await using (var localVerifyContext = _localDb.CreateContext())
        {
            (await localVerifyContext.SyncOperations.CountAsync()).Should().Be(0);
            var cursor = await localVerifyContext.RemoteSyncCursors.SingleAsync();
            cursor.CursorKey.Should().Be("Birds.Pull");
        }
    }

    [Fact]
    public async Task UploadLocalSnapshotAsync_Should_CreateRemoteSchema_WhenDatabaseIsEmpty()
    {
        await using var emptyRemoteDb = new RemoteSqliteInMemoryDb(false);
        var repository = new BirdRepository(_localDb.CreateFactory());
        var bird = Bird.Create(
            Enum.GetValues<BirdsName>()[0],
            "bootstrap remote",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-2)));
        await repository.AddAsync(bird);

        var sut = CreateSut(emptyRemoteDb.CreateFactory());

        var result = await sut.UploadLocalSnapshotAsync(CancellationToken.None);

        result.Status.Should().Be(RemoteSyncRunStatus.Synced);

        await using var remoteVerifyContext = emptyRemoteDb.CreateContext();
        var remoteBird = await remoteVerifyContext.Birds.SingleAsync();
        remoteBird.Description.Should().Be("bootstrap remote");
    }

    private RemoteSyncService CreateSut(IDbContextFactory<RemoteBirdDbContext> remoteFactory)
    {
        return new RemoteSyncService(_localDb.CreateFactory(), remoteFactory, NullLogger<RemoteSyncService>.Instance);
    }

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

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        public RemoteBirdDbContext CreateContext()
        {
            return new RemoteBirdDbContext(_options);
        }

        public IDbContextFactory<RemoteBirdDbContext> CreateFactory()
        {
            return new TestRemoteBirdDbContextFactory(_options);
        }

        private sealed class TestRemoteBirdDbContextFactory(DbContextOptions<RemoteBirdDbContext> options)
            : IDbContextFactory<RemoteBirdDbContext>
        {
            public RemoteBirdDbContext CreateDbContext()
            {
                return new RemoteBirdDbContext(options);
            }

            public ValueTask<RemoteBirdDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(CreateDbContext());
            }
        }
    }
}
