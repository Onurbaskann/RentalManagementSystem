using KiraTakip.Data.Seeding;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Documents;

internal sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> entity)
    {
        entity.Property(b => b.Code).HasMaxLength(50).IsRequired();
        entity.HasIndex(b => b.Code).IsUnique();
        entity.Property(b => b.Name).HasMaxLength(200).IsRequired();
        entity.Property(b => b.Description).HasMaxLength(500);
        entity.Property(b => b.AllowedExtensions).HasMaxLength(200);
        entity.Property(b => b.TargetEntity).HasComment(EnumComment.For<DocumentOwnerType>());
        entity.HasOne(b => b.TemplateDocument)
              .WithMany()
              .HasForeignKey(b => b.TemplateDocumentId)
              .OnDelete(DeleteBehavior.SetNull);

        entity.HasData(SystemSeedData.DocumentTypes);
    }
}
