using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> entity)
    {
        entity.HasIndex(p => new { p.UserId, p.Permission }).IsUnique();
        entity.Property(p => p.UserId).IsRequired();
        entity.Property(p => p.Permission).IsRequired().HasMaxLength(100);
    }
}
