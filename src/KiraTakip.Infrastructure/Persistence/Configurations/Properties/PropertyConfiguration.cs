using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Properties;

internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> entity)
    {
        entity.Property(t => t.Name).HasMaxLength(200);
        entity.Property(t => t.City).HasMaxLength(100);
        entity.Property(t => t.District).HasMaxLength(100);
        entity.Property(t => t.Neighborhood).HasMaxLength(200);
        entity.Property(t => t.Address).HasMaxLength(500);
        entity.Property(t => t.OpenArea).HasPrecision(18, 2);
        entity.Property(t => t.ClosedArea).HasPrecision(18, 2);
        entity.Property(t => t.UnitStructure).HasComment(EnumComment.For<UnitStructure>());
        entity.HasOne(t => t.PropertyType)
              .WithMany()
              .OnDelete(DeleteBehavior.SetNull);
    }
}
