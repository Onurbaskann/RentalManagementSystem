using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.RateSchedule;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class RateScheduleRepository : RepositoryBase<RateSchedule>, IRateScheduleRepository
{
    public RateScheduleRepository(ApplicationDbContext ctx) : base(ctx) { }

    public Task<PagedResult<RateYearSummaryDto>> GetYearSummariesPagedAsync(TableQuery tableQuery)
    {
        var grouped = _dbSet.AsNoTracking().GroupBy(rate => rate.Year);
        var items = grouped
            .OrderByDescending(group => group.Key)
            .Select(group => new RateYearSummaryDto(
                group.Key,
                group.Any(rate => rate.IsActive),
                group.Count()));
        return PagedQuery.CreateAsync(grouped, items, tableQuery);
    }

    public async Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil)
        => await _dbSet.AsNoTracking()
            .Where(k => k.IsActive && k.TenantCategoryId == kategoriId && k.ChargeTypeId == chargeTypeId)
            .OrderByDescending(k => k.Year == donemYil ? 1 : 0)
            .ThenByDescending(k => k.Year)
            .Select(k => new RateValueDto
            {
                CalculationMethod = k.CalculationMethod,
                UnitValue = k.UnitValue,
                KdvRate = k.KdvRate
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentRateRowViewModel>> GetRowsByYearAndCategoryAsync(int year, int? tenantCategoryId)
    {
        var q = _dbSet.AsNoTracking()
            .Where(k => k.Year == year && k.IsActive
                     && k.ChargeType.Behavior != ChargeTypeBehavior.UserManual
                     && k.ChargeType.Behavior != ChargeTypeBehavior.ReservationSpecific);
        if (tenantCategoryId.HasValue)
            q = q.Where(k => k.TenantCategoryId == tenantCategoryId.Value);

        return await q
            .OrderBy(k => k.TenantCategory.Order)
            .ThenBy(k => k.ChargeType.SortOrder)
            .Select(k => new ParentRateRowViewModel
            {
                CategoryName = k.TenantCategory.Name,
                ChargeTypeName = k.ChargeType.Name,
                CalculationMethod = k.CalculationMethod,
                UnitValue = k.UnitValue,
                VatRate = k.KdvRate
            })
            .ToListAsync();
    }
}
