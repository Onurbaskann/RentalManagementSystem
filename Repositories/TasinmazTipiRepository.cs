using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazTipiRepository : BaseRepository<PropertyType>, ITasinmazTipiRepository
{
    public TasinmazTipiRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazTipiListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(k => k.SortOrder).ThenBy(k => k.Name)
            .Select(k => new TasinmazTipiListItemDto
            {
                Id = k.Id,
                Ad = k.Name,
                Kod = k.Code,
                Sira = k.SortOrder,
                Aktif = k.IsActive,
                TekBirimDestekli = k.SupportsSingleUnit,
                CokluBirimDestekli = k.SupportsMultipleUnits
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking()
            .MaxAsync(k => (int?)k.SortOrder) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(k => k.Code == kod && (excludeId == null || k.Id != excludeId));
}
