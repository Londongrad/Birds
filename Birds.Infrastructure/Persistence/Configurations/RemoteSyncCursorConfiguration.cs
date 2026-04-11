using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Birds.Infrastructure.Persistence.Configurations;

public sealed class RemoteSyncCursorConfiguration : IEntityTypeConfiguration<RemoteSyncCursor>
{
    public void Configure(EntityTypeBuilder<RemoteSyncCursor> builder)
    {
        builder.HasKey(cursor => cursor.CursorKey);

        builder.Property(cursor => cursor.CursorKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(cursor => cursor.LastSyncedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}