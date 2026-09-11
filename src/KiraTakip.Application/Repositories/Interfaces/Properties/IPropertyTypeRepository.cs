using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Repositories.Interfaces.Properties;

public interface IPropertyTypeRepository : IRepositoryBase<PropertyType>
{
    Task<List<TasinmazTipiListItemDto>> GetListAsync();
    Task<PagedResult<TasinmazTipiListItemDto>> GetPagedListAsync(TableQuery query);
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<PropertyTypeOptionDto>> GetActiveOptionsAsync();
    Task<PropertyStructureSupportDto?> GetStructureSupportAsync(int propertyTypeId);
}
