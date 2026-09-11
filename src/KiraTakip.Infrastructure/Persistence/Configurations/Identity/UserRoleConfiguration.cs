using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        entity.HasOne(ur => ur.Role)
              .WithMany(r => r.UserRoles)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
