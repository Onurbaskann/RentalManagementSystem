using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ITenantCategoryService
{
    Task<List<CategoryListItemDto>> GetTenantCategoriesAsync();
    Task<PagedResult<CategoryListItemDto>> GetTenantCategoriesPagedAsync(TableQuery query);
    Task<int> GetNextOrderAsync();
    Task<CategoryListItemDto?> GetByIdAsync(GetTenantCategoryByIdInput input);
    Task CreateAsync(CreateTenantCategoryInput input);
    Task UpdateAsync(EditTenantCategoryInput input);
    Task<bool> ToggleStatusAsync(ToggleTenantCategoryStatusInput input);
}
