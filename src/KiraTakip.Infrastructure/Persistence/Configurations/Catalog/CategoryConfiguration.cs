using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.Property(k => k.Name).HasMaxLength(150);
        entity.Property(k => k.Code).HasMaxLength(50);
        entity.HasIndex(k => new { k.Type, k.Code }).IsUnique();
    }
}
