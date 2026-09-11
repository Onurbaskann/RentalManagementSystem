using KiraTakip.Data.Seeding;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Charges;

internal sealed class ChargeTypeConfiguration : IEntityTypeConfiguration<ChargeType>
{
    public void Configure(EntityTypeBuilder<ChargeType> entity)
    {
        entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
        entity.Property(b => b.Code).IsRequired().HasMaxLength(100);
        entity.HasIndex(b => b.Code).IsUnique();
        entity.Property(b => b.Behavior).HasComment(EnumComment.For<ChargeTypeBehavior>());

        entity.HasData(SystemSeedData.ChargeTypes);
    }
}
