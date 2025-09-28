using Birds.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Persistence
{
    public class BirdDbContext(DbContextOptions<BirdDbContext> options) : DbContext(options)
    {
        public DbSet<Bird> Birds => Set<Bird>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BirdDbContext).Assembly);
        }
    }
}