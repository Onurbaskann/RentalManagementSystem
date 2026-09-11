using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> entity)
    {
        entity.Property(d => d.Email).IsRequired().HasMaxLength(256);
        entity.Property(d => d.FullName).HasMaxLength(200);
        entity.Property(d => d.TokenHash).IsRequired().HasMaxLength(128);
        entity.HasIndex(d => new { d.Email, d.Status });
        entity.HasIndex(d => d.TenantId)
              .HasDatabaseName("IX_Davetiyeler_KiraciId_Aktif")
              .HasFilter("[IsDeleted] = 0");
        entity.HasOne(d => d.Role)
              .WithMany()
              .HasForeignKey(d => d.RoleId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
