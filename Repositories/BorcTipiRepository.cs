using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BorcTipiRepository : BaseRepository<BorcTipi>, IBorcTipiRepository
{
    public BorcTipiRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => b.Aktif && b.Davranis == BorcTipiDavranisi.KullaniciManuel)
            .OrderBy(b => b.Sira)
            .Select(b => new BorcTipiLookupDto
            {
                Id = b.Id,
                Ad = b.Ad,
                Kod = b.Kod,
                Davranis = b.Davranis
            })
            .ToListAsync();

    public async Task<BorcTipi?> GetActiveManuelByIdAsync(int id)
        => await _dbSet
            .FirstOrDefaultAsync(b => b.Id == id && b.Aktif && b.Davranis == BorcTipiDavranisi.KullaniciManuel);

    public async Task<List<BorcTipiListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(b => b.Sira)
            .ThenBy(b => b.Ad)
            .Select(b => new BorcTipiListItemDto
            {
                Id = b.Id,
                Ad = b.Ad,
                Kod = b.Kod,
                Davranis = b.Davranis,
                Sira = b.Sira,
                Sistem = b.Sistem,
                Aktif = b.Aktif
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.Sira) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Kod == kod && (excludeId == null || b.Id != excludeId));

    public async Task<List<BorcTipiLookupDto>> GetRezervasyonAdaylariAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => b.Davranis == BorcTipiDavranisi.RezervasyonOzel && b.Aktif)
            .OrderBy(b => b.Sira).ThenBy(b => b.Ad)
            .Select(b => new BorcTipiLookupDto
            {
                Id = b.Id,
                Ad = b.Ad,
                Kod = b.Kod,
                Davranis = b.Davranis
            })
            .ToListAsync();
}
