namespace KiraTakip.Repositories.Interfaces;

public interface IDocumentTypeRepository : IRepositoryBase<DocumentType>
{
    Task<List<DocumentType>> GetListAsync();
    Task<PagedResult<DocumentType>> GetPagedListAsync(TableQuery query);
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<DocumentType>> GetForTargetAsync(KiraTakip.Models.DocumentOwnerType targetEntity, bool requiredOnly);
}
