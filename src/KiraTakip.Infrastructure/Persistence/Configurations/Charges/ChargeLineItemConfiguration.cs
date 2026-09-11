using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Charges;

internal sealed class ChargeLineItemConfiguration : IEntityTypeConfiguration<ChargeLineItem>
{
    public void Configure(EntityTypeBuilder<ChargeLineItem> entity)
    {
        entity.Property(k => k.Description).HasMaxLength(200);
        entity.Property(k => k.UnitValue).HasPrecision(18, 4);
        entity.Property(k => k.CalculationMethod).HasComment(EnumComment.For<CalculationMethod>());
        entity.Property(k => k.SourceType).HasComment(EnumComment.For<LineItemSourceType>());
        entity.Property(k => k.Multiplier).HasPrecision(18, 4);
        entity.Property(k => k.Amount).HasPrecision(18, 2);
        entity.Property(k => k.KdvRate).HasPrecision(5, 2);
        entity.Property(k => k.KdvAmount).HasPrecision(18, 2);
        entity.Property(k => k.TotalAmount).HasPrecision(18, 2);
        entity.Property(k => k.PaidAmount).HasPrecision(18, 2);
        entity.HasOne(k => k.Charge)
              .WithMany(t => t.LineItems)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(k => k.ChargeType)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(k => k.Allocations)
              .WithOne(o => o.ChargeLineItem)
              .HasForeignKey(o => o.ChargeLineItemId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(k => k.ChargeId)
              .HasDatabaseName("IX_TahakkukKalemleri_TahakkukId");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TahakkukKalemleri_Tutarlar_Pozitif", "[Tutar] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0");
            t.HasCheckConstraint("CK_TahakkukKalemleri_KdvOrani", "[KdvOrani] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_TahakkukKalemleri_OdenenLimit", "[OdenenTutar] >= 0 AND [OdenenTutar] <= [ToplamTutar]");
        });
    }
}
