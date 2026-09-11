using KiraTakip.Data.Configurations;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Common;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.Property(a => a.EventType).IsRequired().HasMaxLength(100);
        entity.Property(a => a.EntityType).HasMaxLength(100);
        entity.Property(a => a.EntityId).HasMaxLength(100);
        entity.Property(a => a.IpAddress).HasMaxLength(64);
        entity.Property(a => a.UserAgent).HasMaxLength(500);
        entity.HasIndex(a => new { a.EventType, a.CreatedAt });
        entity.HasIndex(a => new { a.UserId, a.CreatedAt });
        entity.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
