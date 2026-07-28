using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IDocumentService
{
    Task<List<Document>> GetListAsync(GetDocumentsInput input);
    Task<Dictionary<int, List<Document>>> GetListsAsync(GetDocumentsForOwnersInput input);
    Task UploadAsync(UploadDocumentInput input);
    Task<DocumentDownloadResult> DownloadAsync(DownloadDocumentInput input);
    Task<DocumentMutationResult> DeleteAsync(DeleteDocumentInput input);
    Task<List<DocumentType>> GetTypesAsync(GetDocumentTypesInput input);
}
