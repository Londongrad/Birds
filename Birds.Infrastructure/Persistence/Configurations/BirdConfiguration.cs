using Birds.Domain.Common;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Birds.Infrastructure.Persistence.Configurations;

public class BirdConfiguration : IEntityTypeConfiguration<Bird>
{
    public void Configure(EntityTypeBuilder<Bird> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .HasConversion(
                species => BirdSpeciesCodes.ToCode(species),
                value => ParseRequiredSpecies(value))
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(BirdValidationRules.DescriptionMaxLength)
            .IsRequired(false)
            .HasDefaultValue(null);

        builder.Property(b => b.Arrival)
            .IsRequired();

        builder.Property(b => b.Departure)
            .IsRequired(false)
            .HasDefaultValue(null);

        builder.Property(b => b.IsAlive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(b => b.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        builder.Property(b => b.SyncStampUtc)
            .IsRequired()
            .HasConversion(UtcDateTimeConverters.Required)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(b => b.SyncStampUtc);
    }

    private static BirdSpecies ParseRequiredSpecies(string value)
    {
        return BirdSpeciesCodes.Parse(value)
               ?? throw new InvalidOperationException($"Unknown bird species '{value}'.");
    }
}
