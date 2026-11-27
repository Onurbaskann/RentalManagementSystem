using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class PaymentStoreRoutingConfiguration : IEntityTypeConfiguration<PaymentStoreRouting>
{
    public void Configure(EntityTypeBuilder<PaymentStoreRouting> entity)
    {
        entity.HasOne(routing => routing.ChargeType)
            .WithMany()
            .HasForeignKey(routing => routing.ChargeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(routing => routing.Space)
            .WithMany()
            .HasForeignKey(routing => routing.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(routing => routing.Store)
            .WithMany()
            .HasForeignKey(routing => routing.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(routing => routing.ChargeTypeId)
            .IsUnique()
            .HasFilter("[AlanId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_OdemeMagazaYonlendirmeleri_Genel_Aktif");
        entity.HasIndex(routing => new { routing.ChargeTypeId, routing.SpaceId })
            .IsUnique()
            .HasFilter("[AlanId] IS NOT NULL AND [Aktif] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_OdemeMagazaYonlendirmeleri_Alan_Aktif");
        entity.HasIndex(routing => routing.StoreId)
            .HasDatabaseName("IX_OdemeMagazaYonlendirmeleri_MagazaId");
        entity.HasIndex(routing => routing.SpaceId)
            .HasDatabaseName("IX_OdemeMagazaYonlendirmeleri_AlanId");
    }
}
