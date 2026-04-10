using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
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
                BirdsName.Воробей,
                "sync me",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)));
            await repository.AddAsync(bird);

            var sut = CreateSut(_remoteDb.CreateFactory());

            var result = await sut.SyncPendingAsync(CancellationToken.None);

            result.Status.Should().Be(RemoteSyncRunStatus.Synced);
            result.ProcessedCount.Should().Be(1);

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
                BirdsName.Щегол,
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
            result.ProcessedCount.Should().Be(1);

            await using var remoteVerifyContext = _remoteDb.CreateContext();
            (await remoteVerifyContext.Birds.CountAsync()).Should().Be(0);

            await using var localContext = _localDb.CreateContext();
            (await localContext.SyncOperations.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task SyncPendingAsync_WhenRemoteApplyFails_Should_KeepOutbox_And_MarkRetryState()
        {
            await using var brokenRemoteDb = new RemoteSqliteInMemoryDb(ensureCreated: false);
            var repository = new BirdRepository(_localDb.CreateFactory());
            var bird = Bird.Create(
                BirdsName.Щегол,
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
