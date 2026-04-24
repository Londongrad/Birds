using Birds.Domain.Entities;
using Birds.Infrastructure.Persistence.Configurations;
using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Birds.Infrastructure.Persistence;

public sealed class RemoteBirdDbContext(DbContextOptions<RemoteBirdDbContext> options) : DbContext(options)
{
    public DbSet<Bird> Birds => Set<Bird>();
    public DbSet<RemoteBirdTombstone> BirdTombstones => Set<RemoteBirdTombstone>();
    public DbSet<RemoteAppliedSyncOperation> AppliedSyncOperations => Set<RemoteAppliedSyncOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new BirdConfiguration());
        modelBuilder.Entity<Bird>().Ignore(bird => bird.Version);

        modelBuilder.Entity<RemoteBirdTombstone>(builder =>
        {
            builder.HasKey(tombstone => tombstone.BirdId);

            builder.Property(tombstone => tombstone.BirdId)
                .IsRequired();

            builder.Property(tombstone => tombstone.DeletedAtUtc)
                .IsRequired()
                .HasConversion(UtcDateTimeConverters.Required)
                .HasColumnType("timestamp without time zone");

            builder.HasIndex(tombstone => tombstone.DeletedAtUtc);
        });

        modelBuilder.Entity<RemoteAppliedSyncOperation>(builder =>
        {
            builder.HasKey(operation => operation.OperationId);

            builder.Property(operation => operation.OperationId)
                .IsRequired();

            builder.Property(operation => operation.OperationType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(operation => operation.EntityId)
                .IsRequired();

            builder.Property(operation => operation.AppliedAtUtc)
                .IsRequired()
                .HasConversion(UtcDateTimeConverters.Required)
                .HasColumnType("timestamp without time zone");

            builder.HasIndex(operation => operation.EntityId);
            builder.HasIndex(operation => operation.AppliedAtUtc);
        });
    }
}
