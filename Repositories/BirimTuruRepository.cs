using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UnitTypeRepository : BaseRepository<UnitType>, IUnitTypeRepository
{
    public UnitTypeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<UnitTypeListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(b => b.Sira).ThenBy(b => b.Ad)
            .Select(b => new UnitTypeListItemDto
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

    public async Task<bool> HasAktifTahakkukForUnitTypeAsync(int birimTuruId)
        => await _ctx.Tahakkuklar.AsNoTracking()
            .AnyAsync(t => t.Durum != ChargeStatus.Paid
                        && t.Durum != ChargeStatus.Cancelled
                        && t.Birim.UnitTypeId == birimTuruId);

    public async Task<bool> HasPlanlanmisRezervasyonForUnitTypeAsync(int birimTuruId)
        => await _ctx.Rezervasyonlari.AsNoTracking()
            .AnyAsync(r => r.Durum == ReservationStatus.Planned
                        && _ctx.Birimler.Any(b => b.UnitTypeId == birimTuruId && b.Id == r.BirimId));
}
