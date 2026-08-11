using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IDocumentContentRepository : IRepository<DocumentContent, int>
{
    Task<byte[]?> GetContentAsync(int documentId);
}
