using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Persistence
{
    public sealed class RemoteBirdDbContext(DbContextOptions<RemoteBirdDbContext> options) : DbContext(options)
    {
        public DbSet<Bird> Birds => Set<Bird>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new BirdConfiguration());
        }
    }
}
