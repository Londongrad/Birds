using Birds.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Birds.Tests.Infrastructure;

public sealed class SqliteInMemoryDb : IAsyncDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<BirdDbContext> _options;

    public SqliteInMemoryDb()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<BirdDbContext>()
            .UseSqlite(_conn)
            .EnableSensitiveDataLogging()
            .Options;

        using var ctx = new BirdDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await _conn.DisposeAsync();
    }

    public BirdDbContext CreateContext()
    {
        return new BirdDbContext(_options);
    }

    public IDbContextFactory<BirdDbContext> CreateFactory()
    {
        return new TestBirdDbContextFactory(_options);
    }

    private sealed class TestBirdDbContextFactory(DbContextOptions<BirdDbContext> options)
        : IDbContextFactory<BirdDbContext>
    {
        public BirdDbContext CreateDbContext()
        {
            return new BirdDbContext(options);
        }

        public ValueTask<BirdDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(CreateDbContext());
        }
    }
}