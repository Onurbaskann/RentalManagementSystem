using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Properties;

internal sealed class PropertyTypeConfiguration : IEntityTypeConfiguration<PropertyType>
{
    public void Configure(EntityTypeBuilder<PropertyType> entity)
    {
        entity.Property(k => k.Name).HasMaxLength(150);
        entity.Property(k => k.Code).HasMaxLength(50);
        entity.HasIndex(k => k.Code).IsUnique();
    }
}
