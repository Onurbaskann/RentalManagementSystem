using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Properties;

internal sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> entity)
    {
        entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
        entity.Property(b => b.Code).IsRequired().HasMaxLength(100);
        entity.HasIndex(b => b.Code).IsUnique();
        entity.Property(b => b.Usage)
              .HasDefaultValue(UnitTypeUsage.Rentable)
              .HasComment(EnumComment.For<UnitTypeUsage>());

        entity.Property(b => b.RentalMode)
              .HasDefaultValue(UnitRentalMode.UnitBased)
              .HasComment(EnumComment.For<UnitRentalMode>());

        entity.HasOne(b => b.ChargeType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
    }
}
