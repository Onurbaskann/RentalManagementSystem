namespace KiraTakip.Repositories.Interfaces;

public interface IDocumentContentRepository
{
    Task<byte[]?> GetContentAsync(int documentId);
}
