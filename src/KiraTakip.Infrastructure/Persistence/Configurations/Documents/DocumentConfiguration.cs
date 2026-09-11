using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Documents;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> entity)
    {
        entity.Property(b => b.FileName).HasMaxLength(255).IsRequired();
        entity.Property(b => b.MimeType).HasMaxLength(100).IsRequired();
        entity.Property(b => b.Description).HasMaxLength(500);
        entity.Property(b => b.OwnerType).HasComment(EnumComment.For<DocumentOwnerType>());
        entity.HasOne(b => b.DocumentType)
              .WithMany()
              .HasForeignKey(b => b.DocumentTypeId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(b => b.ReplacedByDocument)
              .WithMany()
              .HasForeignKey(b => b.ReplacedByDocumentId)
              .OnDelete(DeleteBehavior.NoAction);
        entity.HasIndex(b => new { b.OwnerType, b.OwnerId, b.IsInvalid, b.IsDeleted });
        entity.HasIndex(b => b.DocumentTypeId);
    }
}
