using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Birds.Infrastructure.Persistence.Configurations;

public sealed class SyncOperationConfiguration : IEntityTypeConfiguration<SyncOperation>
{
    public void Configure(EntityTypeBuilder<SyncOperation> builder)
    {
        builder.HasKey(operation => operation.Id);

        builder.Property(operation => operation.AggregateType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(operation => operation.AggregateId)
            .IsRequired();

        builder.Property(operation => operation.OperationType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(operation => operation.PayloadJson)
            .IsRequired();

        builder.Property(operation => operation.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(operation => operation.UpdatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(operation => operation.LastAttemptAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(operation => operation.LastError)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder.Property(operation => operation.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(operation => new { operation.AggregateType, operation.AggregateId })
            .IsUnique();

        builder.HasIndex(operation => operation.CreatedAtUtc);
    }
}