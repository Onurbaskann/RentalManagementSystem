using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Reservations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> entity)
    {
        entity.Property(r => r.UnitRate).HasPrecision(18, 2);
        entity.Property(r => r.RateAmount).HasPrecision(18, 2);
        entity.Property(r => r.KdvRate).HasPrecision(5, 2);
        entity.Property(r => r.KdvAmount).HasPrecision(18, 2);
        entity.Property(r => r.TotalAmount).HasPrecision(18, 2);
        entity.Property(r => r.Status).HasComment(EnumComment.For<ReservationStatus>());
        entity.Property(r => r.Title).HasMaxLength(200);
        entity.Property(r => r.Description).HasMaxLength(500);
        entity.Property(r => r.Notes).HasMaxLength(2000);
        entity.Property(r => r.InternalNotes).HasMaxLength(2000);
        entity.Property(r => r.LastModificationReason).HasMaxLength(450);
        entity.Property(r => r.RequestedByDisplayNameSnapshot).HasMaxLength(200);
        entity.Property(r => r.RequestedByEmailSnapshot).HasMaxLength(256);
        entity.Property(r => r.RejectionReason).HasMaxLength(450);
        entity.Property(r => r.CancellationReason).HasMaxLength(450);
        entity.HasOne(r => r.Unit)
              .WithMany()
              .HasForeignKey(r => r.UnitId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.Tenant)
              .WithMany()
              .HasForeignKey(r => r.TenantId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.RequestedByUser)
              .WithMany()
              .HasForeignKey(r => r.RequestedByUserId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.ApprovedByUser)
              .WithMany()
              .HasForeignKey(r => r.ApprovedByUserId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.RejectedByUser)
              .WithMany()
              .HasForeignKey(r => r.RejectedByUserId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.CancelledByUser)
              .WithMany()
              .HasForeignKey(r => r.CancelledByUserId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.UnitId, r.StartDate, r.EndDate, r.Status })
              .HasDatabaseName("IX_Rezervasyonlari_BirimTarihDurum_Aktif")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(r => r.TenantId)
              .HasDatabaseName("IX_Rezervasyonlari_KiraciId_Aktif")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Rezervasyonlari_TarihSirasi", "[BitisTarihi] > [BaslangicTarihi]");
            t.HasCheckConstraint("CK_Rezervasyonlari_Tutarlar_Pozitif", "[TarifeTutari] >= 0 AND [ToplamTutar] >= 0 AND ([KdvTutari] IS NULL OR [KdvTutari] >= 0)");
            t.HasCheckConstraint("CK_Rezervasyonlari_KdvOrani", "[KdvOrani] IS NULL OR [KdvOrani] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Rezervasyonlari_Durum", "[Durum] IN (1, 2, 3, 5, 6)");
        });
    }
}
