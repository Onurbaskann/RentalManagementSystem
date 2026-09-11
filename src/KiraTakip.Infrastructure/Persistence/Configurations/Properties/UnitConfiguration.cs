using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Properties;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> entity)
    {
        entity.Property(b => b.Name).HasMaxLength(200);
        entity.Property(b => b.UnitNo).HasMaxLength(50);
        entity.Property(b => b.Area).HasPrecision(18, 2);
        entity.HasOne(b => b.Property)
              .WithMany(t => t.Units)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(b => b.UnitType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
    }
}
