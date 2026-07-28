using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPropertyService
{
    Task<List<PropertyListItemDto>> GetAllAsync(GetPropertiesInput input);
    Task<PropertyDetailDto?> GetDetailsAsync(GetPropertyDetailsInput input);
    Task<PropertyEditDto?> GetForEditAsync(GetPropertyForEditInput input);
    Task<PropertyFormOptionsDto> GetFormOptionsAsync();
    Task<CreatedPropertyDto> CreateAsync(CreatePropertyInput input);
    Task UpdateWithChildrenAsync(UpdatePropertyInput input);
    Task<List<UnitLookupDto>> GetAvailableUnitsAsync(GetAvailableUnitsInput input);
}
