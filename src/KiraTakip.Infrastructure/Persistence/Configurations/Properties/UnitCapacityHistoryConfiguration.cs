using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Properties;

internal sealed class UnitCapacityHistoryConfiguration : IEntityTypeConfiguration<UnitCapacityHistory>
{
    public void Configure(EntityTypeBuilder<UnitCapacityHistory> entity)
    {
        entity.HasOne(h => h.Unit).WithMany().HasForeignKey(h => h.UnitId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(h => new { h.UnitId, h.EffectiveFrom })
              .HasDatabaseName("IX_BirimKontenjanGecmisleri_BirimId_GecerlilikBaslangici");
    }
}
