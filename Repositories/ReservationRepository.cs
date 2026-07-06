using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ReservationRepository : BaseRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<RezervasyonListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
            query = query.Where(r => yetkiliTasinmazIds.Contains(r.Unit.PropertyId));

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RezervasyonListItemDto
            {
                Id = r.Id,
                BirimId = r.UnitId,
                BirimAd = r.Unit.Name,
                TasinmazId = r.Unit.PropertyId,
                TasinmazAd = r.Unit.Property.Name,
                KiraciId = r.TenantId,
                KiraciGosterimAdi = r.Tenant.DisplayName,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalDurationMinutes = r.TotalDurationMinutes,
                FreeDurationMinutes = r.FreeDurationMinutes,
                PaidDurationMinutes = r.PaidDurationMinutes,
                ToplamTutar = r.TotalAmount,
                Durum = r.Status,
                Aciklama = r.Description
            })
            .ToListAsync();
    }

    public async Task<RezervasyonListItemDto?> GetByIdAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RezervasyonListItemDto
            {
                Id = r.Id,
                BirimId = r.UnitId,
                BirimAd = r.Unit.Name,
                TasinmazId = r.Unit.PropertyId,
                TasinmazAd = r.Unit.Property.Name,
                KiraciId = r.TenantId,
                KiraciGosterimAdi = r.Tenant.DisplayName,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalDurationMinutes = r.TotalDurationMinutes,
                FreeDurationMinutes = r.FreeDurationMinutes,
                PaidDurationMinutes = r.PaidDurationMinutes,
                ToplamTutar = r.TotalAmount,
                Durum = r.Status,
                Aciklama = r.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsConflictAsync(int birimId, DateTime baslangic, DateTime bitis)
    {
        return await _dbSet.AnyAsync(r =>
            r.UnitId == birimId &&
            r.Status != ReservationStatus.Cancelled &&
            r.StartDate < bitis &&
            r.EndDate > baslangic);
    }

    public async Task<RezervasyonTarife?> GetAktifTarifeForBirimAsync(int birimId)
    {
        return await _ctx.RezervasyonTarifeler
            .Where(k => k.IsActive && k.UnitId == birimId)
            .FirstOrDefaultAsync();
    }

    public async Task<RezervasyonTarife?> GetGenelTarifeAsync(int birimTuruId, int yil)
    {
        return await _ctx.RezervasyonTarifeler
            .Where(g => g.UnitId == null && g.UnitTypeId == birimTuruId && g.IsActive && g.Yil == yil)
            .FirstOrDefaultAsync();
    }

    public async Task<List<RezervasyonTarife>> GetUcretKurallariAsync()
    {
        return await _ctx.RezervasyonTarifeler
            .Include(k => k.Unit!).ThenInclude(b => b!.Property)
            .Where(k => k.UnitId != null)
            .OrderBy(k => k.Id)
            .ToListAsync();
    }

    public async Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id)
    {
        return await _ctx.RezervasyonTarifeler
            .Include(k => k.Unit)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task AddUcretKuralAsync(RezervasyonTarife kural)
    {
        await _ctx.RezervasyonTarifeler.AddAsync(kural);
    }

    public async Task AddTahakkukAsync(Charge tahakkuk)
    {
        await _ctx.Charges.AddAsync(tahakkuk);
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
