using KiraTakip.Models.Dtos.PropertyType;

namespace KiraTakip.Services.Interfaces;

public interface IPropertyTypeService
{
    Task<List<PropertyTypeListItemDto>> GetListAsync();
    Task<int> GetMaxSortOrderAsync();
    Task CreateAsync(CreateInput input);
    Task<PropertyType?> GetByIdAsync(int id);
    Task UpdateAsync(int id, EditInput input);
    Task<bool> ToggleStatusAsync(int id);
}
