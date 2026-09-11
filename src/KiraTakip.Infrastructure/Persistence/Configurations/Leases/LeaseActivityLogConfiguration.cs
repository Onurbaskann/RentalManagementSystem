using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseActivityLogConfiguration : IEntityTypeConfiguration<LeaseActivityLog>
{
    public void Configure(EntityTypeBuilder<LeaseActivityLog> entity)
    {
        entity.Property(g => g.Description).HasMaxLength(1000);
        entity.Property(g => g.ActivityType).HasComment(EnumComment.For<LeaseActivityType>());
        entity.Property(g => g.OldRentAmount).HasPrecision(18, 2);
        entity.Property(g => g.NewRentAmount).HasPrecision(18, 2);
        entity.Property(g => g.InflationRate).HasPrecision(5, 2);
        entity.Property(g => g.KdvRate).HasPrecision(5, 2);
        entity.Property(g => g.KdvAmount).HasPrecision(18, 2);
        entity.Property(g => g.KdvIncludedAmount).HasPrecision(18, 2);
        entity.HasOne(g => g.Lease)
              .WithMany(s => s.ActivityLog)
              .HasForeignKey(g => g.LeaseId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
