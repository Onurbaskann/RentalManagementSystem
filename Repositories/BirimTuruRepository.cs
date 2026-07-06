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
                BorcTipiId = b.ChargeTypeId,
                BorcTipiAd = b.ChargeType != null ? b.ChargeType.Name : null,
                Aktif = b.IsActive
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.Sira) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Kod == kod && (excludeId == null || b.Id != excludeId));

    public async Task<bool> AnyAktifByBorcTipiIdAsync(int borcTipiId, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.ChargeTypeId == borcTipiId && b.IsActive && (excludeId == null || b.Id != excludeId));

    public async Task<bool> HasAktifTahakkukForUnitTypeAsync(int birimTuruId)
        => await _ctx.Charges.AsNoTracking()
            .AnyAsync(t => t.Status != ChargeStatus.Paid
                        && t.Status != ChargeStatus.Cancelled
                        && t.Unit.UnitTypeId == birimTuruId);

    public async Task<bool> HasPlanlanmisRezervasyonForUnitTypeAsync(int birimTuruId)
        => await _ctx.Reservations.AsNoTracking()
            .AnyAsync(r => r.Status == ReservationStatus.Planned
                        && _ctx.Units.Any(b => b.UnitTypeId == birimTuruId && b.Id == r.UnitId));
}
