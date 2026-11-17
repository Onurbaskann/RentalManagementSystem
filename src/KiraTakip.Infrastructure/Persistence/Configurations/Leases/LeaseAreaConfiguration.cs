using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseAreaConfiguration : IEntityTypeConfiguration<LeaseArea>
{
    public void Configure(EntityTypeBuilder<LeaseArea> entity)
    {
        entity.Property(a => a.RentedArea).HasPrecision(18, 2);
        entity.Property(a => a.AllocatedArea).HasPrecision(18, 2);
        entity.HasOne(a => a.Lease).WithOne(s => s.LeaseArea)
              .HasForeignKey<LeaseArea>(a => a.LeaseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => a.LeaseId).IsUnique()
              .HasDatabaseName("UX_SozlesmeAlanlari_SozlesmeId_Silinmemis").HasFilter("[IsDeleted] = 0");
        entity.ToTable(t => t.HasCheckConstraint("CK_SozlesmeAlanlari_Alanlar",
            "[KiralananAlan] > 0 AND [FizikselAlan] > 0 AND [FizikselAlan] <= [KiralananAlan]"));
    }
}
