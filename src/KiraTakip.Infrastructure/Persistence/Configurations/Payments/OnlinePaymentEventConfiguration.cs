using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class OnlinePaymentEventConfiguration : IEntityTypeConfiguration<OnlinePaymentEvent>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentEvent> entity)
    {
        entity.Property(e => e.EventType).HasComment(EnumComment.For<OnlinePaymentEventType>());
        entity.Property(e => e.ProviderResponseCode).HasMaxLength(20);
        entity.Property(e => e.ProviderTransactionStatus).HasMaxLength(20);
        entity.Property(e => e.SafeSummary).HasMaxLength(1000);
        entity.HasOne(e => e.OnlinePaymentTransaction)
            .WithMany(t => t.Events)
            .HasForeignKey(e => e.OnlinePaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => e.OnlinePaymentTransactionId)
            .HasDatabaseName("IX_SanalPosIslemOlaylari_SanalPosIslemiId");
    }
}
