using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceTypeDefaultAttributeConfiguration : IEntityTypeConfiguration<SpaceTypeDefaultAttribute>
{
    public void Configure(EntityTypeBuilder<SpaceTypeDefaultAttribute> entity)
    {
        entity.HasOne(d => d.SpaceType)
              .WithMany(t => t.DefaultAttributes)
              .HasForeignKey(d => d.SpaceTypeId)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(d => d.AttributeDefinition)
              .WithMany()
              .HasForeignKey(d => d.AttributeDefinitionId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.SpaceTypeId, d.AttributeDefinitionId })
              .IsUnique()
              .HasDatabaseName("UX_AlanTuruVarsayilanOzellikleri_TurOzellik")
              .HasFilter("[IsDeleted] = 0");
    }
}
