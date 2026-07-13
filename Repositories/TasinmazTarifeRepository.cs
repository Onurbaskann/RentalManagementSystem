using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazTarifeRepository : BaseRepository<PropertyRateOverride>, ITasinmazTarifeRepository
{
    public TasinmazTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<PropertyRateOverride>> GetByPropertyIdAsync(int propertyId)
        => await _dbSet
            .AsNoTracking()
            .Where(f => f.PropertyId == propertyId)
            .ToListAsync();

    public async Task<List<Category>> GetKiraciKategorileriAsync()
        => await _ctx.Kategoriler
            .AsNoTracking()
            .Where(k => k.Type == CategoryType.Tenant)
            .OrderBy(k => k.Name)
            .ToListAsync();

    public async Task<List<ChargeType>> GetBorcTipleriMatrisIcinAsync()
        => await _ctx.ChargeTypes
            .AsNoTracking()
            .Where(b => b.Behavior != ChargeTypeBehavior.UserManual
                     && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

    public async Task<List<PropertyRateOverride>> GetForHiyerarsiAsync(int propertyId, int? kategoriId)
    {
        IQueryable<PropertyRateOverride> q = _dbSet
            .AsNoTracking()
            .Include(f => f.TenantCategory)
            .Include(f => f.ChargeType)
            .Where(f => f.PropertyId == propertyId && f.IsActive);

        if (kategoriId.HasValue)
            q = q.Where(f => f.TenantCategoryId == kategoriId.Value);

        return await q
            .OrderBy(f => f.TenantCategory.Order)
            .ThenBy(f => f.ChargeType.SortOrder)
            .ToListAsync();
    }

    public async Task<RateValueDto?> GetRateAsync(int propertyId, int kategoriId, int chargeTypeId)
        => await _dbSet.AsNoTracking()
            .Where(f => f.PropertyId == propertyId
                     && f.TenantCategoryId == kategoriId
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
