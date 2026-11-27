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
        entity.HasOne(r => r.Space)
              .WithMany()
              .HasForeignKey(r => r.SpaceId)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(r => r.ChargeType)
              .WithMany()
              .HasForeignKey(r => r.ChargeTypeId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => r.SpaceId)
              .IsUnique()
              .HasDatabaseName("UX_RezervasyonTarifeler_AlanId_Silinmemis")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_RezervasyonTarifeler_Degerler_Pozitif",
                "[PeriyotUcreti] >= 0 AND [UcretsizSureDakika] >= 0 AND [UcretlendirmePeriyoduDakika] > 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
    }
}
