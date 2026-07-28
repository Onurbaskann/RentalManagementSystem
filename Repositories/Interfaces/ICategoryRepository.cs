using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ICategoryRepository : IBaseRepository<Category>
{
    Task<List<CategoryListItemDto>> GetListByTipiAsync(CategoryType tipi);
    Task<Category?> GetByIdAndTipiAsync(int id, CategoryType tipi);
    Task<int> GetMaxSiraByTipiAsync(CategoryType tipi);
    Task<bool> KodExistsByTipiAsync(CategoryType tipi, string kod, int? excludeId = null);
    Task<List<Category>> GetTenantPricingCategoriesAsync();
}
