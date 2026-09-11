using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Pricing;

internal sealed class UnitRateConfiguration : IEntityTypeConfiguration<UnitRate>
{
    public void Configure(EntityTypeBuilder<UnitRate> entity)
    {
        entity.Property(r => r.UnitValue).HasPrecision(18, 4);
        entity.Property(r => r.KdvRate).HasPrecision(5, 2);
        entity.HasIndex(r => new { r.UnitId, r.TenantCategoryId, r.ChargeTypeId })
              .IsUnique()
              .HasDatabaseName("UX_BirimTarifeler_BirimKategoriBorc")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_BirimTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
        entity.HasOne(r => r.Unit)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(r => r.TenantCategory)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.ChargeType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
    }
}
