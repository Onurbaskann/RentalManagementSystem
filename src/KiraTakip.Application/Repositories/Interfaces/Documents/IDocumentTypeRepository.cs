using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Documents;

public interface IDocumentTypeRepository : IRepositoryBase<DocumentType>
{
    Task<List<DocumentType>> GetListAsync();
    Task<PagedResult<DocumentType>> GetPagedListAsync(TableQuery query);
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<DocumentType>> GetForTargetAsync(Models.Enums.DocumentOwnerType targetEntity, bool requiredOnly);
}
