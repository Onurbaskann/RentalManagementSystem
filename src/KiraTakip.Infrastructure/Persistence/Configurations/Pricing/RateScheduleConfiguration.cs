using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Pricing;

internal sealed class RateScheduleConfiguration : IEntityTypeConfiguration<RateSchedule>
{
    public void Configure(EntityTypeBuilder<RateSchedule> entity)
    {
        entity.Property(k => k.UnitValue).HasPrecision(18, 4);
        entity.Property(k => k.KdvRate).HasPrecision(5, 2);
        entity.Property(k => k.CalculationMethod).HasComment(EnumComment.For<CalculationMethod>());
        entity.HasIndex(k => new { k.Year, k.TenantCategoryId, k.ChargeTypeId })
              .IsUnique()
              .HasDatabaseName("UX_GenelTarifeler_YilKategoriBorc")
              .HasFilter("[IsDeleted] = 0");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_GenelTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
        });
        entity.HasOne(k => k.TenantCategory)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(k => k.ChargeType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
    }
}
