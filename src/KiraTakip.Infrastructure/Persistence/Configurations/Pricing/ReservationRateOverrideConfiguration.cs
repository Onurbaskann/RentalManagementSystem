using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Pricing;

internal sealed class ReservationRateOverrideConfiguration : IEntityTypeConfiguration<ReservationRateOverride>
{
    public void Configure(EntityTypeBuilder<ReservationRateOverride> entity)
    {
        entity.Property(r => r.PeriodRate).HasPrecision(18, 2);
        entity.Property(r => r.KdvRate).HasPrecision(5, 2);
        entity.Property(r => r.Description).HasMaxLength(300);
        entity.HasOne(r => r.Unit)
              .WithMany()
              .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(r => r.UnitType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.UnitTypeId, r.Year })
              .IsUnique()
              .HasDatabaseName("UX_RezervasyonTarifeler_BirimTuruYil_GenelKural")
              .HasFilter("[BirimId] IS NULL");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_RezervasyonTarife_BirimOrYilTuru",
                "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)");
            t.HasCheckConstraint(
                "CK_RezervasyonTarifeler_Degerler_Pozitif",
                "[PeriyotUcreti] >= 0 AND [UcretsizSureDakika] >= 0 AND [UcretlendirmePeriyoduDakika] > 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
    }
}
