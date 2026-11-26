using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceTypeConfiguration : IEntityTypeConfiguration<SpaceType>
{
    public void Configure(EntityTypeBuilder<SpaceType> entity)
    {
        entity.Property(t => t.Code).HasMaxLength(50);
        entity.Property(t => t.Name).HasMaxLength(150);
        entity.Property(t => t.DefaultUsage).HasComment(EnumComment.For<SpaceUsage>());
        entity.HasIndex(t => t.Code)
              .IsUnique()
              .HasDatabaseName("UX_AlanTurleri_Kod")
              .HasFilter("[IsDeleted] = 0");
    }
}
