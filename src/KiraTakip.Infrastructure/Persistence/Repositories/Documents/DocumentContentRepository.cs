using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Documents;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Documents;

public class DocumentContentRepository(ApplicationDbContext context)
    : Repository<DocumentContent, int>(context, content => content.DocumentId), IDocumentContentRepository
{
    public Task<byte[]?> GetContentAsync(int documentId)
        => _dbSet
            .AsNoTracking()
            .Where(content => content.DocumentId == documentId)
            .Select(content => content.Content)
            .FirstOrDefaultAsync();
}
