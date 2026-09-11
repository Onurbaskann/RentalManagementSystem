using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Charges;

internal sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> entity)
    {
        entity.Property(t => t.ExpectedAmount).HasPrecision(18, 2);
        entity.Property(t => t.Status).HasComment(EnumComment.For<ChargeStatus>());
        entity.Property(t => t.SourceType).HasComment(EnumComment.For<ChargeSourceType>());
        entity.Property(t => t.KdvAmount).HasPrecision(18, 2);
        entity.Property(t => t.TotalAmount).HasPrecision(18, 2);
        entity.Property(t => t.PaidAmount).HasPrecision(18, 2);
        entity.Property(t => t.CancellationNote).HasMaxLength(500);
        entity.HasOne(t => t.Tenant)
              .WithMany()
              .HasForeignKey(t => t.TenantId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(t => t.Unit)
              .WithMany()
              .HasForeignKey(t => t.UnitId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(t => t.Lease)
              .WithMany()
              .HasForeignKey(t => t.LeaseId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(t => t.Reservation)
              .WithMany()
              .HasForeignKey(t => t.ReservationId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => new { t.LeaseId, t.PeriodStart })
              .IsUnique()
              .HasDatabaseName("UX_Tahakkuklar_SozlesmeDonem_TekTahakkuk")
              .HasFilter("[SozlesmeId] IS NOT NULL AND [KaynakTipi] = 1 AND [IsDeleted] = 0");
        entity.HasIndex(t => t.TenantId)
              .HasDatabaseName("IX_Tahakkuklar_KiraciId_Aktif")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(t => t.UnitId)
              .HasDatabaseName("IX_Tahakkuklar_BirimId_Aktif")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(t => t.ReservationId)
              .IsUnique()
              .HasDatabaseName("UX_Tahakkuklar_RezervasyonId_TekTahakkuk")
              .HasFilter("[RezervasyonId] IS NOT NULL AND [IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Tahakkuklar_TarihSirasi", "[DonemBitisi] >= [DonemBaslangici]");
            t.HasCheckConstraint("CK_Tahakkuklar_Tutarlar_Pozitif", "[BeklenenTutar] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0 AND [OdenenTutar] >= 0");
            t.HasCheckConstraint("CK_Tahakkuklar_OdenenLimit", "[OdenenTutar] <= [ToplamTutar]");
        });
    }
}
