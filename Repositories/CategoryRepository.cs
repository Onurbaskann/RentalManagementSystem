using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
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
}
