using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyRepository : IBaseRepository<Property>
{
    Task<List<PropertyListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PropertyDetailDto?> GetDetailsAsync(int id);
    Task<Property?> GetWithUnitsTrackedAsync(int id);
    Task<bool> CanChangeUnitStructureAsync(int propertyId);
}
