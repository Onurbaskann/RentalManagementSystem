using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Payments;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> entity)
    {
        entity.Property(store => store.Code).IsRequired().HasMaxLength(100);
        entity.Property(store => store.Name).IsRequired().HasMaxLength(200);
        entity.Property(store => store.Description).HasMaxLength(500);
        entity.HasIndex(store => store.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Magazalar_Kod_Silinmemis");
    }
}
