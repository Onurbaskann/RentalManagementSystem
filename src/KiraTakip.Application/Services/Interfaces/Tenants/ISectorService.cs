using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Sector;

namespace KiraTakip.Services.Interfaces.Tenants;

public interface ISectorService
{
    Task<List<CategoryListItemDto>> GetSectorsAsync();
    Task<PagedResult<CategoryListItemDto>> GetSectorsPagedAsync(TableQuery query);
    Task<int> GetNextOrderAsync();
    Task<CategoryListItemDto?> GetByIdAsync(GetSectorByIdInput input);
    Task CreateAsync(CreateSectorInput input);
    Task UpdateAsync(EditSectorInput input);
    Task<bool> ToggleStatusAsync(ToggleSectorStatusInput input);
}
