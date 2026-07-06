using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazTarifeRepository : BaseRepository<TasinmazTarife>, ITasinmazTarifeRepository
{
    public TasinmazTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazTarife>> GetByPropertyIdAsync(int propertyId)
        => await _dbSet
            .AsNoTracking()
            .Where(f => f.PropertyId == propertyId)
            .ToListAsync();

    public async Task<List<Kategori>> GetKiraciKategorileriAsync()
        => await _ctx.Kategoriler
            .AsNoTracking()
            .Where(k => k.Tipi == KategoriTipi.Tenant)
            .OrderBy(k => k.Ad)
            .ToListAsync();

    public async Task<List<ChargeType>> GetBorcTipleriMatrisIcinAsync()
        => await _ctx.ChargeTypes
            .AsNoTracking()
            .Where(b => b.Behavior != ChargeTypeBehavior.UserManual
                     && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

    public async Task<List<TasinmazTarife>> GetForHiyerarsiAsync(int propertyId, int? kategoriId)
    {
        IQueryable<TasinmazTarife> q = _dbSet
            .AsNoTracking()
            .Include(f => f.KiraciKategori)
            .Include(f => f.ChargeType)
            .Where(f => f.PropertyId == propertyId && f.IsActive);

        if (kategoriId.HasValue)
            q = q.Where(f => f.KiraciKategoriId == kategoriId.Value);

        return await q
            .OrderBy(f => f.KiraciKategori.Sira)
            .ThenBy(f => f.ChargeType.SortOrder)
            .ToListAsync();
    }

    public async Task<RateValueDto?> GetRateAsync(int propertyId, int kategoriId, int chargeTypeId)
        => await _dbSet.AsNoTracking()
            .Where(f => f.PropertyId == propertyId
                     && f.KiraciKategoriId == kategoriId
                     && f.ChargeTypeId == chargeTypeId
                     && f.IsActive)
            .Select(f => new RateValueDto
            {
                CalculationMethod = f.CalculationMethod,
                UnitValue = f.UnitValue,
                KdvRate = f.KdvRate
            })
            .FirstOrDefaultAsync();
}
