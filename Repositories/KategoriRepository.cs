using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class KategoriRepository : BaseRepository<Kategori>, IKategoriRepository
{
    public KategoriRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<KategoriListItemDto>> GetListByTipiAsync(KategoriTipi tipi)
        => await _dbSet.AsNoTracking()
            .Where(k => k.Tipi == tipi)
            .OrderBy(k => k.Sira).ThenBy(k => k.Ad)
            .Select(k => new KategoriListItemDto
            {
                Id = k.Id,
                Tipi = k.Tipi,
                Ad = k.Ad,
                Kod = k.Kod,
                Sira = k.Sira,
                Aktif = k.Aktif
            })
            .ToListAsync();

    public async Task<Kategori?> GetByIdAndTipiAsync(int id, KategoriTipi tipi)
        => await _dbSet.FirstOrDefaultAsync(k => k.Id == id && k.Tipi == tipi);

    public async Task<int> GetMaxSiraByTipiAsync(KategoriTipi tipi)
        => await _dbSet.AsNoTracking()
            .Where(k => k.Tipi == tipi)
            .MaxAsync(k => (int?)k.Sira) ?? 0;

    public async Task<bool> KodExistsByTipiAsync(KategoriTipi tipi, string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(k => k.Tipi == tipi && k.Kod == kod && (excludeId == null || k.Id != excludeId));
}
