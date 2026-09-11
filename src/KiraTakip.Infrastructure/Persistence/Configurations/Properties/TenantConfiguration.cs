using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Properties;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> entity)
    {
        entity.Property(k => k.TenantNo).HasMaxLength(20);
        entity.HasIndex(k => k.TenantNo).IsUnique();
        entity.Property(k => k.Name).HasMaxLength(200);
        entity.Property(k => k.Phone).HasMaxLength(30);
        entity.Property(k => k.Email).HasMaxLength(200);
        entity.Property(k => k.TaxNo).HasMaxLength(20);
        entity.HasIndex(k => k.TaxNo)
              .IsUnique()
              .HasDatabaseName("UX_Kiraciler_VergiNo")
              .HasFilter("[VergiNo] IS NOT NULL AND [VergiNo] <> ''");
        entity.HasOne(k => k.TenantCategory)
              .WithMany()
              .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(k => k.Sector)
              .WithMany()
              .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
