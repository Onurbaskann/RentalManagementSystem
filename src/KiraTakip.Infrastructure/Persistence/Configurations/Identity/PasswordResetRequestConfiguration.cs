using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Identity;

internal sealed class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> entity)
    {
        entity.Property(t => t.UserId).IsRequired();
        entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        entity.Property(t => t.RequestIp).HasMaxLength(64);
        entity.HasIndex(t => new { t.UserId, t.Status });
    }
}
