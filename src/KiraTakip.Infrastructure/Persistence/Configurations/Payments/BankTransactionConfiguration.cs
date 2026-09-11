using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> entity)
    {
        entity.Property(b => b.TransactionAmount).HasPrecision(18, 2);
        entity.Property(b => b.Description).HasMaxLength(500);
        entity.Property(b => b.MatchStatus).HasComment(EnumComment.For<BankMatchStatus>());
        entity.Property(b => b.SenderIban).HasMaxLength(50);
        entity.Property(b => b.SenderInfo).HasMaxLength(200);
        entity.Property(b => b.BankReferenceNo).HasMaxLength(100);
        entity.Property(b => b.BankCode).HasMaxLength(20);
        entity.HasIndex(b => b.BankReferenceNo)
              .IsUnique()
              .HasDatabaseName("UX_BankaHareketleri_BankaReferansNo")
              .HasFilter("[BankaReferansNo] IS NOT NULL AND [IsDeleted] = 0");
        entity.HasOne(b => b.StoreAccount)
              .WithMany()
              .HasForeignKey(b => b.StoreAccountId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(b => b.StoreAccountId)
              .HasDatabaseName("IX_BankaHareketleri_MagazaHesapBilgisiId");
    }
}
