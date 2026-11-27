using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseGroupConfiguration : IEntityTypeConfiguration<LeaseGroup>
{
    public void Configure(EntityTypeBuilder<LeaseGroup> entity)
    {
        entity.HasOne(g => g.Space).WithMany().HasForeignKey(g => g.SpaceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(g => g.PreviousGroup).WithMany().HasForeignKey(g => g.PreviousGroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
