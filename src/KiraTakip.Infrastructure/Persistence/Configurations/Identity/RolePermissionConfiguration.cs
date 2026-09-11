using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> entity)
    {
        entity.Property(rp => rp.Permission).IsRequired().HasMaxLength(150);
        entity.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();
        entity.HasOne(rp => rp.Role)
              .WithMany(r => r.RolePermissions)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
