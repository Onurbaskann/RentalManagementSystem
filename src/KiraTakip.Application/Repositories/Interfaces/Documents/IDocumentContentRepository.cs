using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Documents;

public interface IDocumentContentRepository : IRepository<DocumentContent, int>
{
    Task<byte[]?> GetContentAsync(int documentId);
}
