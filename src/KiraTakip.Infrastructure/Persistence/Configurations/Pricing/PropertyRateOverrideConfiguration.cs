using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Pricing;

internal sealed class PropertyRateOverrideConfiguration : IEntityTypeConfiguration<PropertyRateOverride>
{
    public void Configure(EntityTypeBuilder<PropertyRateOverride> entity)
    {
        entity.Property(f => f.UnitValue).HasPrecision(18, 2);
        entity.Property(f => f.KdvRate).HasPrecision(5, 2);
        entity.HasIndex(f => new { f.PropertyId, f.TenantCategoryId, f.ChargeTypeId })
              .IsUnique()
              .HasDatabaseName("UX_TasinmazTarifeler_TasinmazKategoriBorc")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TasinmazTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
        entity.HasOne(f => f.Property)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(f => f.TenantCategory)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(f => f.ChargeType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
    }
}
