using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class OnlinePaymentTransactionConfiguration : IEntityTypeConfiguration<OnlinePaymentTransaction>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentTransaction> entity)
    {
        entity.Property(t => t.ProviderCode).IsRequired().HasMaxLength(50);
        entity.Property(t => t.MerchantPaymentId).IsRequired().HasMaxLength(100);
        entity.Property(t => t.ProviderTransactionId).HasMaxLength(100);
        entity.Property(t => t.Amount).HasPrecision(18, 2);
        entity.Property(t => t.Currency).IsRequired().HasColumnType("char(3)");
        entity.Property(t => t.Status).HasComment(EnumComment.For<OnlinePaymentTransactionStatus>());
        entity.Property(t => t.ResponseCode).HasMaxLength(20);
        entity.Property(t => t.TransactionStatus).HasMaxLength(20);
        entity.Property(t => t.ErrorCode).HasMaxLength(50);
        entity.Property(t => t.SafeMessage).HasMaxLength(500);
        entity.HasOne(t => t.ChargeLineItem)
            .WithMany()
            .HasForeignKey(t => t.ChargeLineItemId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(t => t.StoreAccount)
            .WithMany()
            .HasForeignKey(t => t.StoreAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(t => t.InitiatedByUser)
            .WithMany()
            .HasForeignKey(t => t.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(t => t.PaymentAllocation)
            .WithMany()
            .HasForeignKey(t => t.PaymentAllocationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => t.MerchantPaymentId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_SanalPosIslemleri_UyeIsyeriOdemeNo_Silinmemis");
        entity.HasIndex(t => new { t.ChargeLineItemId, t.Status })
            .HasDatabaseName("IX_SanalPosIslemleri_TahakkukKalemiId_Durum");
        entity.HasIndex(t => t.StoreAccountId)
            .HasDatabaseName("IX_SanalPosIslemleri_MagazaHesapBilgisiId");
    }
}
