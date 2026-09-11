using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Pricing;

internal sealed class LeaseRateOverrideConfiguration : IEntityTypeConfiguration<LeaseRateOverride>
{
    public void Configure(EntityTypeBuilder<LeaseRateOverride> entity)
    {
        entity.Property(r => r.UnitValue).HasPrecision(18, 4);
        entity.Property(r => r.KdvRate).HasPrecision(5, 2);
        entity.HasIndex(r => new { r.LeaseId, r.ChargeTypeId })
              .IsUnique()
              .HasDatabaseName("UX_SozlesmeTarifeler_SozlesmeBorc")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SozlesmeTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
        entity.HasOne(r => r.Lease)
              .WithMany(s => s.LeaseRateOverrides)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(r => r.ChargeType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
    }
}
