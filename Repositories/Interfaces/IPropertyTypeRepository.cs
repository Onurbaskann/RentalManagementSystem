using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyTypeRepository : IRepositoryBase<PropertyType>
{
    Task<List<TasinmazTipiListItemDto>> GetListAsync();
    Task<PagedResult<TasinmazTipiListItemDto>> GetPagedListAsync(TableQuery query);
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<PropertyTypeOptionDto>> GetActiveOptionsAsync();
    Task<PropertyStructureSupportDto?> GetStructureSupportAsync(int propertyTypeId);
}
