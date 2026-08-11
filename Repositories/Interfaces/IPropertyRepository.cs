using KiraTakip.Models.Dtos;

using KiraTakip.Models.Common;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyRepository : IRepositoryBase<Property>
{
    Task<List<PropertyListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PagedResult<PropertyListItemDto>> GetPagedListAsync(
        TableQuery query,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PropertyDetailDto?> GetDetailsAsync(int id);
    Task<Property?> GetWithUnitsTrackedAsync(int id);
    Task<bool> CanChangeUnitStructureAsync(int propertyId);
}
