using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Documents;

internal sealed class DocumentContentConfiguration : IEntityTypeConfiguration<DocumentContent>
{
    public void Configure(EntityTypeBuilder<DocumentContent> entity)
    {
        entity.HasKey(i => i.DocumentId);
        entity.HasOne(i => i.Document)
              .WithOne(b => b.Content)
              .HasForeignKey<DocumentContent>(i => i.DocumentId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
