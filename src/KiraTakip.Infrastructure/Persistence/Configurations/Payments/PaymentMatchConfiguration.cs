using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class PaymentMatchConfiguration : IEntityTypeConfiguration<PaymentMatch>
{
    public void Configure(EntityTypeBuilder<PaymentMatch> entity)
    {
        entity.Property(e => e.MatchType)
              .HasComment(EnumComment.For<KiraTakip.Models.Enums.MatchType>());
        entity.HasOne(e => e.PaymentAllocation)
              .WithMany(o => o.BankMatches)
              .HasForeignKey(e => e.PaymentAllocationId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.BankTransaction)
              .WithMany(b => b.Matches)
              .HasForeignKey(e => e.BankTransactionId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => e.BankTransactionId)
              .IsUnique()
              .HasDatabaseName("UX_OdemeBankaEslesmeleri_BankaHareketi_Birebir")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(e => e.PaymentAllocationId)
              .IsUnique()
              .HasDatabaseName("UX_OdemeBankaEslesmeleri_TahakkukOdeme_Birebir")
              .HasFilter("[IsDeleted] = 0");
    }
}
