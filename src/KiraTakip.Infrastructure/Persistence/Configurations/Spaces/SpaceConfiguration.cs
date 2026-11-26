using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Spaces;

internal sealed class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> entity)
    {
        entity.Property(s => s.Code).HasMaxLength(50);
        entity.Property(s => s.Name).HasMaxLength(200);
        entity.Property(s => s.Description).HasMaxLength(1000);
        entity.Property(s => s.Path).HasMaxLength(450).IsUnicode(false).UseCollation("Latin1_General_BIN2");
        entity.Property(s => s.Area).HasPrecision(18, 2);
        entity.Property(s => s.Usage).HasComment(EnumComment.For<SpaceUsage>());

        entity.HasIndex(s => s.Path).HasDatabaseName("IX_Alanlar_Yol");
        entity.HasIndex(s => new { s.ParentId, s.Code })
              .IsUnique()
              .HasDatabaseName("UX_Alanlar_UstAlan_Kod")
              .HasFilter("[IsDeleted] = 0");
        entity.HasIndex(s => s.Code)
              .IsUnique()
              .HasDatabaseName("UX_Alanlar_Kok_Kod")
              .HasFilter("[UstAlanId] IS NULL AND [IsDeleted] = 0");

        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Alanlar_UstAlan", "[UstAlanId] IS NULL OR [UstAlanId] <> [Id]");
            t.HasCheckConstraint("CK_Alanlar_Bayraklar",
                "(([OnTahsiseAcik] = 0 AND [Paylasimli] = 0) OR [Kullanim] = 1) AND NOT ([OnTahsiseAcik] = 1 AND [Paylasimli] = 1)");
            t.HasCheckConstraint("CK_Alanlar_Derinlik", "[Derinlik] BETWEEN 0 AND 19");
            t.HasCheckConstraint("CK_Alanlar_Yuzolcumu", "[Yuzolcumu] > 0");
        });

        entity.HasOne(s => s.Parent)
              .WithMany(s => s.Children)
              .HasForeignKey(s => s.ParentId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(s => s.SpaceType)
              .WithMany()
              .HasForeignKey(s => s.SpaceTypeId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
