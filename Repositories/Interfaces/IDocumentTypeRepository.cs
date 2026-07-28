namespace KiraTakip.Repositories.Interfaces;

public interface IDocumentTypeRepository : IBaseRepository<DocumentType>
{
    Task<List<DocumentType>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<DocumentType>> GetForTargetAsync(KiraTakip.Models.DocumentOwnerType targetEntity, bool requiredOnly);
}
