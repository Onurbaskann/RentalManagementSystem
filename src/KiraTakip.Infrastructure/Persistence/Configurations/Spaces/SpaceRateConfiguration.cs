using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceRateConfiguration : IEntityTypeConfiguration<SpaceRate>
{
    public void Configure(EntityTypeBuilder<SpaceRate> entity)
    {
        entity.Property(r => r.UnitValue).HasPrecision(18, 4);
        entity.Property(r => r.KdvRate).HasPrecision(5, 2);
        entity.Property(r => r.CalculationMethod).HasComment(EnumComment.For<CalculationMethod>());
        entity.HasIndex(r => new { r.SpaceId, r.TenantCategoryId, r.ChargeTypeId })
              .IsUnique()
              .HasDatabaseName("UX_AlanTarifeleri_AlanKategoriBorc")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_AlanTarifeleri_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
        entity.HasOne(r => r.Space)
              .WithMany()
              .HasForeignKey(r => r.SpaceId)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(r => r.TenantCategory)
              .WithMany()
              .HasForeignKey(r => r.TenantCategoryId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.ChargeType)
              .WithMany()
              .HasForeignKey(r => r.ChargeTypeId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
