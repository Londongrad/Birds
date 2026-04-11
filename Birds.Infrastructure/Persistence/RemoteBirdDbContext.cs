using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence.Configurations;
using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Persistence
{
    public sealed class RemoteBirdDbContext(DbContextOptions<RemoteBirdDbContext> options) : DbContext(options)
    {
        public DbSet<Bird> Birds => Set<Bird>();
        public DbSet<RemoteBirdTombstone> BirdTombstones => Set<RemoteBirdTombstone>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new BirdConfiguration());

            modelBuilder.Entity<RemoteBirdTombstone>(builder =>
            {
                builder.HasKey(tombstone => tombstone.BirdId);

                builder.Property(tombstone => tombstone.BirdId)
                    .IsRequired();

                builder.Property(tombstone => tombstone.DeletedAtUtc)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                builder.HasIndex(tombstone => tombstone.DeletedAtUtc);
            });
        }
    }
}
