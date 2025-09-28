using Birds.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Birds.Infrastructure.Persistence.Configurations
{
    public class BirdConfiguration : IEntityTypeConfiguration<Bird>
    {
        public void Configure(EntityTypeBuilder<Bird> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Name)
                .HasConversion<string>()  // enum → string
                .IsRequired();

            builder.Property(b => b.Description)
                .HasMaxLength(200)
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
                .HasDefaultValue(DateTime.UtcNow);

            builder.Property(b => b.UpdatedAt)
                .HasDefaultValue(DateTime.UtcNow);
        }
    }
}