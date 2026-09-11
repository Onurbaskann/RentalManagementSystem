using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.TenantCategory;

namespace KiraTakip.Services.Interfaces.Tenants;

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
