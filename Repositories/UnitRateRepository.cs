using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UnitRateRepository : BaseRepository<UnitRate>, IUnitRateRepository
{
    public UnitRateRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<RateValueDto?> GetRateAsync(int unitId, int tenantCategoryId, int chargeTypeId)
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId == unitId
                     && r.TenantCategoryId == tenantCategoryId
                     && r.ChargeTypeId == chargeTypeId
                     && r.IsActive)
            .Select(r => new RateValueDto
            {
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentTarifeKartViewModel>> GetByBirimForKartAsync(int unitId, int? tenantCategoryId)
    {
        var q = _dbSet.AsNoTracking().Where(r => r.UnitId == unitId);
        if (tenantCategoryId.HasValue)
            q = q.Where(r => r.TenantCategoryId == tenantCategoryId.Value);

        var rateler = await q
            .OrderBy(r => r.TenantCategory.Order)
            .ThenBy(r => r.ChargeType.SortOrder)
            .Select(r => new ParentTarifeSatir
            {
                CategoryName = r.TenantCategory.Name,
                ChargeTypeName = r.ChargeType.Name,
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            })
            .ToListAsync();

        if (rateler.Count == 0) return new List<ParentTarifeKartViewModel>();

        return new List<ParentTarifeKartViewModel>
        {
            new ParentTarifeKartViewModel
            {
                SourceName = "Unit Tarifesi",
                Rows = rateler
            }
        };
    }
}
