using Birds.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Birds.Tests.Infrastructure
{
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

        public BirdDbContext CreateContext() => new BirdDbContext(_options);

        public async ValueTask DisposeAsync() => await _conn.DisposeAsync();
    }
}