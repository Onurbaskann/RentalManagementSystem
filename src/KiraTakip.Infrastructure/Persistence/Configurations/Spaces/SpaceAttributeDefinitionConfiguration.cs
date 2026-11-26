using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceAttributeDefinitionConfiguration : IEntityTypeConfiguration<SpaceAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<SpaceAttributeDefinition> entity)
    {
        entity.Property(d => d.Code).HasMaxLength(50);
        entity.Property(d => d.Name).HasMaxLength(150);
        entity.Property(d => d.DataType).HasComment(EnumComment.For<AttributeDataType>());
        entity.HasIndex(d => d.Code)
              .IsUnique()
              .HasDatabaseName("UX_AlanOzellikTanimlari_Kod")
              .HasFilter("[IsDeleted] = 0");
    }
}
