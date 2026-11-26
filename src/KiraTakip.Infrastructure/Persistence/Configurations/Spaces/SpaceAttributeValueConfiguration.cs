using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceAttributeValueConfiguration : IEntityTypeConfiguration<SpaceAttributeValue>
{
    public void Configure(EntityTypeBuilder<SpaceAttributeValue> entity)
    {
        entity.Property(v => v.Value).HasMaxLength(1000);
        entity.HasOne(v => v.Space)
              .WithMany(s => s.AttributeValues)
              .HasForeignKey(v => v.SpaceId)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(v => v.AttributeDefinition)
              .WithMany()
              .HasForeignKey(v => v.AttributeDefinitionId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(v => new { v.SpaceId, v.AttributeDefinitionId })
              .IsUnique()
              .HasDatabaseName("UX_AlanOzellikDegerleri_AlanOzellik")
              .HasFilter("[IsDeleted] = 0");
    }
}
