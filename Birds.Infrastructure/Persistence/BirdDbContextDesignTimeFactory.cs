using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Birds.Infrastructure.Persistence;

public sealed class BirdDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BirdDbContext>
{
    public BirdDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BirdDbContext>()
            .UseSqlite("Data Source=birds-design-time.db")
            .Options;

        return new BirdDbContext(options);
    }
}
