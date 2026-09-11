using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> entity)
    {
        entity.Property(s => s.Status).HasComment(EnumComment.For<LeaseStatus>());
        entity.HasOne(s => s.Unit)
              .WithMany(b => b.Leases)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(s => s.Tenant)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
        // Not: "Bir birimde tek devam-eden aktif sözleşme" kuralı tarih koşulu içerdiğinden
        // filtered index ile ifade edilemez (SQL Server GETDATE() destegi yok);
        // kontrol uygulama katmanında yapılır (LeaseController.Create).
        entity.HasIndex(s => s.UnitId, "IX_Lease_UnitId")
              .HasDatabaseName("IX_Sozlesmeler_BirimId");
        entity.HasIndex(s => s.TenantId)
              .HasDatabaseName("IX_Sozlesmeler_KiraciId_Aktif")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(s => s.UnitId, "UX_Lease_UnitId_OpenApplication")
              .IsUnique()
              .HasDatabaseName("UX_Sozlesmeler_BirimId_AcikBasvuru")
              .HasFilter("[IsDeleted] = 0 AND [Durum] IN (4, 5)");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Sozlesmeler_TarihSirasi", "[BitisTarihi] > [BaslangicTarihi]");
            t.HasCheckConstraint("CK_Sozlesmeler_VadeGunu", "[VadeGunu] BETWEEN 1 AND 31");
        });
    }
}
