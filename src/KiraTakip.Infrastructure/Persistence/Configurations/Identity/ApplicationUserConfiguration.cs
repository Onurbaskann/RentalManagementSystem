using KiraTakip.Data.Configurations;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.HasOne<Tenant>()
              .WithMany()
              .HasForeignKey(u => u.TenantId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ApplicationUser_SuperAdmin_KiraciYok", "[IsSuperAdmin] = 0 OR [KiraciId] IS NULL");
        });
    }
}
