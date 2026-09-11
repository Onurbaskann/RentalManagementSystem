using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Common;

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> entity)
    {
        entity.Property(setting => setting.Key).HasMaxLength(150).IsRequired();
        entity.Property(setting => setting.Value).HasMaxLength(2000).IsRequired();
        entity.HasIndex(setting => setting.Key)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_SistemAyarlari_Anahtar_Aktif");
    }
}
