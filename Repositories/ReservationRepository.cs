using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ReservationRepository : BaseRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<ReservationListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliPropertyIds != null)
            query = query.Where(r => yetkiliPropertyIds.Contains(r.Unit.PropertyId));

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReservationListItemDto
            {
                Id = r.Id,
                UnitId = r.UnitId,
                UnitName = r.Unit.Name,
                PropertyId = r.Unit.PropertyId,
                PropertyName = r.Unit.Property.Name,
                TenantId = r.TenantId,
                TenantDisplayName = r.Tenant.DisplayName,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalDurationMinutes = r.TotalDurationMinutes,
                FreeDurationMinutes = r.FreeDurationMinutes,
                PaidDurationMinutes = r.PaidDurationMinutes,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                Description = r.Description
            })
            .ToListAsync();
    }

    public async Task<ReservationListItemDto?> GetByIdAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservationListItemDto
            {
                Id = r.Id,
                UnitId = r.UnitId,
                UnitName = r.Unit.Name,
                PropertyId = r.Unit.PropertyId,
                PropertyName = r.Unit.Property.Name,
                TenantId = r.TenantId,
                TenantDisplayName = r.Tenant.DisplayName,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalDurationMinutes = r.TotalDurationMinutes,
                FreeDurationMinutes = r.FreeDurationMinutes,
                PaidDurationMinutes = r.PaidDurationMinutes,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                Description = r.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsConflictAsync(int unitId, DateTime baslangic, DateTime bitis)
    {
        return await _dbSet.AnyAsync(r =>
            r.UnitId == unitId &&
            r.Status != ReservationStatus.Cancelled &&
            r.StartDate < bitis &&
            r.EndDate > baslangic);
    }

    public async Task<ReservationRateOverride?> GetAktifTarifeForBirimAsync(int unitId)
    {
        return await _ctx.RezervasyonTarifeler
            .Where(k => k.IsActive && k.UnitId == unitId)
            .FirstOrDefaultAsync();
    }

    public async Task<ReservationRateOverride?> GetGenelTarifeAsync(int unitTypeId, int yil)
    {
        return await _ctx.RezervasyonTarifeler
            .Where(g => g.UnitId == null && g.UnitTypeId == unitTypeId && g.IsActive && g.Year == yil)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ReservationRateOverride>> GetUcretKurallariAsync()
    {
        return await _ctx.RezervasyonTarifeler
            .Include(k => k.Unit!).ThenInclude(b => b!.Property)
            .Where(k => k.UnitId != null)
            .OrderBy(k => k.Id)
            .ToListAsync();
    }

    public async Task<ReservationRateOverride?> GetUcretKuralByIdAsync(int id)
    {
        return await _ctx.RezervasyonTarifeler
            .Include(k => k.Unit)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task AddUcretKuralAsync(ReservationRateOverride kural)
    {
        await _ctx.RezervasyonTarifeler.AddAsync(kural);
    }

    public async Task AddTahakkukAsync(Charge charge)
    {
        await _ctx.Charges.AddAsync(charge);
    }

    public async Task<ChargeType?> ResolveRezervasyonBorcTipiAsync(int? preferredBorcTipiId)
    {
        if (preferredBorcTipiId.HasValue)
        {
            var bt = await _ctx.ChargeTypes
                .FirstOrDefaultAsync(b => b.Id == preferredBorcTipiId.Value && b.IsActive);
            if (bt != null) return bt;
        }

        return await _ctx.ChargeTypes
            .FirstOrDefaultAsync(b => b.Behavior == ChargeTypeBehavior.ReservationSpecific && b.IsActive);
    }
}
