using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ISectorService
{
    Task<List<CategoryListItemDto>> GetSectorsAsync();
    Task<int> GetNextOrderAsync();
    Task<CategoryListItemDto?> GetByIdAsync(GetSectorByIdInput input);
    Task CreateAsync(CreateSectorInput input);
    Task UpdateAsync(EditSectorInput input);
    Task<bool> ToggleStatusAsync(ToggleSectorStatusInput input);
}
