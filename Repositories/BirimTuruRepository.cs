using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BirimTuruRepository : BaseRepository<BirimTuru>, IBirimTuruRepository
{
    public BirimTuruRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<BirimTuruListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(b => b.Sira).ThenBy(b => b.Ad)
            .Select(b => new BirimTuruListItemDto
            {
                Id = b.Id,
                Ad = b.Ad,
                Kod = b.Kod,
                Sira = b.Sira,
                KiralanabilirMi = b.KiralanabilirMi,
                RezervasyonYapilabilirMi = b.RezervasyonYapilabilirMi,
                BorcTipiId = b.BorcTipiId,
                BorcTipiAd = b.BorcTipi != null ? b.BorcTipi.Ad : null,
                Aktif = b.Aktif
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.Sira) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Kod == kod && (excludeId == null || b.Id != excludeId));

    public async Task<bool> AnyAktifByBorcTipiIdAsync(int borcTipiId, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.BorcTipiId == borcTipiId && b.Aktif && (excludeId == null || b.Id != excludeId));

    public async Task<bool> HasAktifTahakkukForBirimTuruAsync(int birimTuruId)
        => await _ctx.Tahakkuklar.AsNoTracking()
            .AnyAsync(t => t.Durum != TahakkukDurumu.TamOdendi
                        && t.Durum != TahakkukDurumu.IptalEdildi
                        && _ctx.Birimler.Any(b => b.BirimTuruId == birimTuruId
                            && (_ctx.Sozlesmeler.Any(s => s.BirimId == b.Id && s.Id == t.KiraSozlesmesiId)
                                || _ctx.Rezervasyonlari.Any(r => r.BirimId == b.Id && r.TahakkukId == t.Id))));

    public async Task<bool> HasPlanlanmisRezervasyonForBirimTuruAsync(int birimTuruId)
        => await _ctx.Rezervasyonlari.AsNoTracking()
            .AnyAsync(r => r.Durum == RezervasyonDurumu.Planlandi
                        && _ctx.Birimler.Any(b => b.BirimTuruId == birimTuruId && b.Id == r.BirimId));
}
