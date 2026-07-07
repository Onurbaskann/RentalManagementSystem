using KiraTakip.Models.Entities;
using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public interface IDocumentService
{
    Task<List<Document>> GetListAsync(DocumentOwnerType ownerType, int ownerId);

    Task<Document> UploadAsync(DocumentOwnerType ownerType, int ownerId, int documentTypeId,
        string fileName, string mimeType, byte[] content, string? description = null, bool invalidateOld = true);

    Task<(Document Meta, byte[] Icerik)> DownloadAsync(int documentId);

    Task DeleteAsync(int documentId);

    Task<List<DocumentType>> GetTurlerAsync(DocumentOwnerType targetEntity, bool sadeceDogru = false);
}
