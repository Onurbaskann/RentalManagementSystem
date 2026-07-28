using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyTypeRepository : IBaseRepository<PropertyType>
{
    Task<List<TasinmazTipiListItemDto>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<PropertyTypeOptionDto>> GetActiveOptionsAsync();
    Task<PropertyStructureSupportDto?> GetStructureSupportAsync(int propertyTypeId);
}
