using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
        entity.Property(r => r.Description).HasMaxLength(500);
        entity.Property(r => r.Scope).HasComment(EnumComment.For<RoleScope>());
        entity.HasIndex(r => new { r.Scope, r.TenantId, r.Name }).IsUnique();
        entity.HasOne<Tenant>()
              .WithMany()
              .HasForeignKey(r => r.TenantId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
