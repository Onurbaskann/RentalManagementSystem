using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class UserPermissionScopeConfiguration : IEntityTypeConfiguration<UserPermissionScope>
{
    public void Configure(EntityTypeBuilder<UserPermissionScope> entity)
    {
        entity.Property(k => k.UserId).IsRequired().HasMaxLength(450);
        entity.HasIndex(k => new { k.UserId, k.ScopeType, k.ScopeId }).IsUnique();
    }
}
