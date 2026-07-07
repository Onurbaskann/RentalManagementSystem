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
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new UnitTypeListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                SortOrder = b.SortOrder,
                CanBeRented = b.CanBeRented,
                CanBeReserved = b.CanBeReserved,
                ChargeTypeId = b.ChargeTypeId,
                ChargeTypeName = b.ChargeType != null ? b.ChargeType.Name : null,
                IsActive = b.IsActive
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.SortOrder) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Code == kod && (excludeId == null || b.Id != excludeId));

    public async Task<bool> AnyAktifByBorcTipiIdAsync(int chargeTypeId, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.ChargeTypeId == chargeTypeId && b.IsActive && (excludeId == null || b.Id != excludeId));

    public async Task<bool> HasAktifTahakkukForUnitTypeAsync(int unitTypeId)
        => await _ctx.Charges.AsNoTracking()
            .AnyAsync(t => t.Status != ChargeStatus.Paid
                        && t.Status != ChargeStatus.Cancelled
                        && t.Unit.UnitTypeId == unitTypeId);

    public async Task<bool> HasPlanlanmisRezervasyonForUnitTypeAsync(int unitTypeId)
        => await _ctx.Reservations.AsNoTracking()
            .AnyAsync(r => r.Status == ReservationStatus.Planned
                        && _ctx.Units.Any(b => b.UnitTypeId == unitTypeId && b.Id == r.UnitId));
}
