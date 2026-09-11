using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> entity)
    {
        entity.Property(o => o.Amount).HasPrecision(18, 2);
        entity.Property(o => o.RejectionReason).HasMaxLength(500);
        entity.Property(o => o.PosReferenceNo).HasMaxLength(100);
        entity.Property(o => o.PaymentChannel).HasComment(EnumComment.For<PaymentChannel>());
        entity.Property(o => o.PaymentSourceType).HasComment(EnumComment.For<PaymentSourceType>());
        entity.Property(o => o.Status).HasComment(EnumComment.For<PaymentStatus>());
        entity.HasOne(o => o.Charge)
              .WithMany(t => t.Allocations)
              .HasForeignKey(o => o.ChargeId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(o => o.Lease)
              .WithMany()
              .HasForeignKey(o => o.LeaseId)
              .OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(o => o.GirenUser)
              .WithMany()
              .HasForeignKey(o => o.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(o => o.OnaylayanUser)
              .WithMany()
              .HasForeignKey(o => o.ApprovedByUserId)
              .OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(o => o.StoreAccount)
              .WithMany()
              .HasForeignKey(o => o.StoreAccountId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(o => new { o.ChargeLineItemId, o.Status })
              .HasDatabaseName("IX_TahakkukOdemeleri_TahakkukKalemiId_Durum")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(o => o.StoreAccountId)
              .HasDatabaseName("IX_TahakkukOdemeleri_MagazaHesapBilgisiId");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TahakkukOdemeler_Tutar_Pozitif", "[Tutar] > 0");
        });
    }
}
