using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceAddressConfiguration : IEntityTypeConfiguration<SpaceAddress>
{
    public void Configure(EntityTypeBuilder<SpaceAddress> entity)
    {
        entity.Property(a => a.City).HasMaxLength(100);
        entity.Property(a => a.District).HasMaxLength(100);
        entity.Property(a => a.Neighborhood).HasMaxLength(200);
        entity.Property(a => a.Address).HasMaxLength(500);
        entity.HasOne(a => a.Space)
              .WithOne(s => s.Address)
              .HasForeignKey<SpaceAddress>(a => a.SpaceId)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(a => a.SpaceId)
              .IsUnique()
              .HasDatabaseName("UX_AlanAdresleri_AlanId")
              .HasFilter("[IsDeleted] = 0");
    }
}
