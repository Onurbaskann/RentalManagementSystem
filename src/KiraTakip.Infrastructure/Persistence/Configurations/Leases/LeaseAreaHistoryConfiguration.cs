using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseAreaHistoryConfiguration : IEntityTypeConfiguration<LeaseAreaHistory>
{
    public void Configure(EntityTypeBuilder<LeaseAreaHistory> entity)
    {
        entity.Property(h => h.AllocatedArea).HasPrecision(18, 2);
        entity.HasOne(h => h.LeaseArea).WithMany().HasForeignKey(h => h.LeaseAreaId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(h => new { h.LeaseAreaId, h.EffectiveFrom })
              .HasDatabaseName("IX_SozlesmeAlanGecmisleri_SozlesmeAlanId_GecerlilikBaslangici");
        entity.ToTable(t => t.HasCheckConstraint("CK_SozlesmeAlanGecmisleri_FizikselAlan", "[FizikselAlan] > 0"));
    }
}
