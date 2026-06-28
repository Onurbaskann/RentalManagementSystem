using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazTipiRepository : BaseRepository<TasinmazTipi>, ITasinmazTipiRepository
{
    public TasinmazTipiRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazTipiListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(k => k.Sira).ThenBy(k => k.Ad)
            .Select(k => new TasinmazTipiListItemDto
            {
                Id = k.Id,
                Ad = k.Ad,
                Kod = k.Kod,
                Sira = k.Sira,
                Aktif = k.Aktif,
                TekParcaDestekli = k.TekParcaDestekli,
                BirimBazliDestekli = k.BirimBazliDestekli
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking()
            .MaxAsync(k => (int?)k.Sira) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(k => k.Kod == kod && (excludeId == null || k.Id != excludeId));
}
