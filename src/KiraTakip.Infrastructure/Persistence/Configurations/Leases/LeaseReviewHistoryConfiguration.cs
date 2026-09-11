using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Leases;

internal sealed class LeaseReviewHistoryConfiguration : IEntityTypeConfiguration<LeaseReviewHistory>
{
    public void Configure(EntityTypeBuilder<LeaseReviewHistory> entity)
    {
        entity.Property(g => g.ActionType).HasComment(EnumComment.For<LeaseReviewActionType>());
        entity.Property(g => g.FromStatus).HasComment(EnumComment.For<LeaseStatus>());
        entity.Property(g => g.ToStatus).HasComment(EnumComment.For<LeaseStatus>());
        entity.Property(g => g.Explanation).HasMaxLength(1000);
        entity.Property(g => g.ActorUserId).IsRequired();
        entity.HasOne(g => g.Lease)
              .WithMany(s => s.ReviewHistory)
              .HasForeignKey(g => g.LeaseId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(g => g.ActorUser)
              .WithMany()
              .HasForeignKey(g => g.ActorUserId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(g => new { g.LeaseId, g.ActionDate })
              .HasDatabaseName("IX_SozlesmeIncelemeGecmisleri_SozlesmeId_IslemTarihi");
        entity.HasIndex(g => g.ActorUserId)
              .HasDatabaseName("IX_SozlesmeIncelemeGecmisleri_IslemYapanKullaniciId");
    }
}
