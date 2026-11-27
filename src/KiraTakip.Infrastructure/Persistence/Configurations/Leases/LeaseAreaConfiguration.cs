using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseAreaConfiguration : IEntityTypeConfiguration<LeaseArea>
{
    public void Configure(EntityTypeBuilder<LeaseArea> entity)
    {
        entity.Property(a => a.RentedArea).HasPrecision(18, 2);
        entity.Property(a => a.SnapshotArea).HasPrecision(18, 2);
        entity.Property(a => a.SnapshotCode).HasMaxLength(50);
        entity.Property(a => a.SnapshotName).HasMaxLength(200);
        entity.Property(a => a.SnapshotTypeName).HasMaxLength(150);
        entity.Property(a => a.SnapshotUsage).HasComment(EnumComment.For<SpaceUsage>());
        entity.HasOne(a => a.Lease).WithMany(s => s.Areas)
              .HasForeignKey(a => a.LeaseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Space).WithMany()
              .HasForeignKey(a => a.SpaceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.LeaseId, a.SpaceId }).IsUnique()
              .HasDatabaseName("UX_SozlesmeAlanlari_SozlesmeId_AlanId_Silinmemis").HasFilter("[IsDeleted] = 0");
        entity.ToTable(t => t.HasCheckConstraint("CK_SozlesmeAlanlari_KiralananAlan", "[KiralananAlan] > 0"));
    }
}
