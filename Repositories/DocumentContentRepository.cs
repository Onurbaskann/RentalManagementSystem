using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class DocumentContentRepository(ApplicationDbContext context) : IDocumentContentRepository
{
    public Task<byte[]?> GetContentAsync(int documentId)
        => context.DocumentContents
            .AsNoTracking()
            .Where(content => content.DocumentId == documentId)
            .Select(content => content.Content)
            .FirstOrDefaultAsync();
}
