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
        entity.HasOne(routing => routing.Property)
            .WithMany()
            .HasForeignKey(routing => routing.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(routing => routing.Unit)
            .WithMany()
            .HasForeignKey(routing => routing.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(routing => routing.Store)
            .WithMany()
            .HasForeignKey(routing => routing.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(routing => routing.ChargeTypeId)
            .IsUnique()
            .HasFilter("[TasinmazId] IS NULL AND [BirimId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_OdemeMagazaYonlendirmeleri_Genel_Aktif");
        entity.HasIndex(routing => new { routing.ChargeTypeId, routing.PropertyId })
            .IsUnique()
            .HasFilter("[TasinmazId] IS NOT NULL AND [BirimId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_OdemeMagazaYonlendirmeleri_Tasinmaz_Aktif");
        entity.HasIndex(routing => new { routing.ChargeTypeId, routing.UnitId })
            .IsUnique()
            .HasFilter("[TasinmazId] IS NULL AND [BirimId] IS NOT NULL AND [Aktif] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_OdemeMagazaYonlendirmeleri_Birim_Aktif");
        entity.HasIndex(routing => routing.StoreId)
            .HasDatabaseName("IX_OdemeMagazaYonlendirmeleri_MagazaId");
        entity.HasIndex(routing => routing.PropertyId)
            .HasDatabaseName("IX_OdemeMagazaYonlendirmeleri_TasinmazId");
        entity.HasIndex(routing => routing.UnitId)
            .HasDatabaseName("IX_OdemeMagazaYonlendirmeleri_BirimId");

        entity.ToTable(table => table.HasCheckConstraint(
            "CK_OdemeMagazaYonlendirmeleri_Kapsam",
            "([TasinmazId] IS NULL AND [BirimId] IS NULL) OR " +
            "([TasinmazId] IS NOT NULL AND [BirimId] IS NULL) OR " +
            "([TasinmazId] IS NULL AND [BirimId] IS NOT NULL)"));
    }
}
