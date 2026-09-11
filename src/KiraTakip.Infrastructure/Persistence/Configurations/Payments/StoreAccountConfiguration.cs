using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class StoreAccountConfiguration : IEntityTypeConfiguration<StoreAccount>
{
    public void Configure(EntityTypeBuilder<StoreAccount> entity)
    {
        entity.Property(account => account.ProviderCode).IsRequired().HasMaxLength(50);
        entity.Property(account => account.Currency).IsRequired().HasColumnType("char(3)");
        entity.Property(account => account.MerchantId).IsRequired().HasMaxLength(200);
        entity.Property(account => account.MerchantUser).IsRequired().HasMaxLength(200);
        entity.Property(account => account.ProtectedMerchantPassword).IsRequired();
        entity.HasIndex(account => account.StoreId)
            .IsUnique()
            .HasFilter("[Aktif] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_MagazaHesapBilgileri_Magaza_Aktif");
        entity.HasIndex(account => new { account.StoreId, account.ValidFrom })
            .HasDatabaseName("IX_MagazaHesapBilgileri_MagazaId_GecerlilikBaslangici");
        entity.HasOne(account => account.Store)
            .WithMany(store => store.Accounts)
            .HasForeignKey(account => account.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table => table.HasCheckConstraint(
            "CK_MagazaHesapBilgileri_Gecerlilik",
            "[GecerlilikBitisi] IS NULL OR [GecerlilikBitisi] >= [GecerlilikBaslangici]"));
    }
}
