using KiraTakip.Models;
using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IDocumentRepository : IRepositoryBase<Document>
{
    Task<List<Document>> GetListAsync(DocumentOwnerType ownerType, int ownerId);
    Task<List<Document>> GetListAsync(DocumentOwnerType ownerType, IReadOnlyCollection<int> ownerIds);
    Task<Document?> GetCurrentAsync(DocumentOwnerType ownerType, int ownerId, int documentTypeId);
    Task<Document?> GetMetadataAsync(int documentId);
    Task<Document?> FindAsync(int documentId);
    Task SoftDeleteByOwnerAsync(DocumentOwnerType ownerType, int ownerId);
}
