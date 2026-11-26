using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceHistoryConfiguration : IEntityTypeConfiguration<SpaceHistory>
{
    public void Configure(EntityTypeBuilder<SpaceHistory> entity)
    {
        entity.Property(h => h.Field).HasComment(EnumComment.For<SpaceHistoryField>());
        entity.Property(h => h.OldValue).HasMaxLength(500);
        entity.Property(h => h.NewValue).HasMaxLength(500);
        entity.Property(h => h.UserId).HasMaxLength(450);
        entity.HasOne(h => h.Space)
              .WithMany(s => s.History)
              .HasForeignKey(h => h.SpaceId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(h => new { h.SpaceId, h.ChangedAt }).HasDatabaseName("IX_AlanTarihcesi_Alan_Tarih");
    }
}
