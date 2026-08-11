using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<CategoryListItemDto>> GetListByTipiAsync(CategoryType tipi)
        => await _dbSet.AsNoTracking()
            .Where(k => k.Type == tipi)
            .OrderBy(k => k.Order).ThenBy(k => k.Name)
            .Select(k => new CategoryListItemDto
            {
                Id = k.Id,
                Type = k.Type,
                Name = k.Name,
                Code = k.Code,
                Order = k.Order,
                IsActive = k.IsActive
            })
            .ToListAsync();

    public async Task<Category?> GetByIdAndTipiAsync(int id, CategoryType tipi)
        => await _dbSet.FirstOrDefaultAsync(k => k.Id == id && k.Type == tipi);

    public async Task<int> GetMaxSiraByTipiAsync(CategoryType tipi)
        => await _dbSet.AsNoTracking()
            .Where(k => k.Type == tipi)
            .MaxAsync(k => (int?)k.Order) ?? 0;

    public async Task<bool> KodExistsByTipiAsync(CategoryType tipi, string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(k => k.Type == tipi && k.Code == kod && (excludeId == null || k.Id != excludeId));

    public Task<List<Category>> GetTenantPricingCategoriesAsync()
        => _dbSet.AsNoTracking()
            .Where(category => category.Type == CategoryType.Tenant)
            .OrderBy(category => category.Name)
            .ToListAsync();

    public Task<PagedResult<CategoryListItemDto>> GetPagedListByTypeAsync(CategoryType type, TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking().Where(category => category.Type == type);
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(category => category.Name.Contains(search) || category.Code.Contains(search));
        }
        var items = query
            .OrderBy(category => category.Order).ThenBy(category => category.Name).ThenBy(category => category.Id)
            .Select(category => new CategoryListItemDto
            {
                Id = category.Id,
                Type = category.Type,
                Name = category.Name,
                Code = category.Code,
                Order = category.Order,
                IsActive = category.IsActive
            });
        return GetPagedResultAsync(query, items, tableQuery);
    }
}
