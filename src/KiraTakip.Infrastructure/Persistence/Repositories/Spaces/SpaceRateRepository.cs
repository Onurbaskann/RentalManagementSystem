using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceRateRepository(ApplicationDbContext ctx) : RepositoryBase<SpaceRate>(ctx), ISpaceRateRepository
{
    public Task<List<SpaceRate>> GetBySpaceAsync(int spaceId)
        => _dbSet.AsNoTracking().Where(r => r.SpaceId == spaceId)
            .OrderBy(r => r.TenantCategoryId).ThenBy(r => r.ChargeTypeId).ToListAsync();

    public async Task<List<(int SpaceId, KiraTakip.Models.Dtos.RateValueDto Rate)>> GetActiveRatesAsync(
        IReadOnlyCollection<int> spaceIds, int tenantCategoryId, int chargeTypeId)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(r => spaceIds.Contains(r.SpaceId)
                     && r.TenantCategoryId == tenantCategoryId
                     && r.ChargeTypeId == chargeTypeId
                     && r.IsActive
                     && !r.IsDeleted)
            .Select(r => new { r.SpaceId, r.CalculationMethod, r.UnitValue, r.KdvRate })
            .ToListAsync();

        return rows.Select(r => (r.SpaceId, new KiraTakip.Models.Dtos.RateValueDto
        {
            CalculationMethod = r.CalculationMethod,
            UnitValue = r.UnitValue,
            KdvRate = r.KdvRate
        })).ToList();
    }

    public async Task<List<(int SpaceId, int TenantCategoryId, int ChargeTypeId, KiraTakip.Models.Dtos.RateValueDto Rate)>> GetActiveRatesAsync(
        IReadOnlyCollection<int> spaceIds, IReadOnlyCollection<int> tenantCategoryIds, IReadOnlyCollection<int> chargeTypeIds)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(r => spaceIds.Contains(r.SpaceId)
                     && tenantCategoryIds.Contains(r.TenantCategoryId)
                     && chargeTypeIds.Contains(r.ChargeTypeId)
                     && r.IsActive
                     && !r.IsDeleted)
            .Select(r => new { r.SpaceId, r.TenantCategoryId, r.ChargeTypeId, r.CalculationMethod, r.UnitValue, r.KdvRate })
            .ToListAsync();

        return rows.Select(r => (r.SpaceId, r.TenantCategoryId, r.ChargeTypeId, new KiraTakip.Models.Dtos.RateValueDto
        {
            CalculationMethod = r.CalculationMethod,
            UnitValue = r.UnitValue,
            KdvRate = r.KdvRate
        })).ToList();
    }

    public async Task<List<(int SpaceId, KiraTakip.Models.Dtos.RateHierarchy.ParentRateRowDto Row)>> GetParentRowsAsync(
        IReadOnlyCollection<int> spaceIds, int? tenantCategoryId)
    {
        var query = _dbSet.AsNoTracking()
            .Where(r => spaceIds.Contains(r.SpaceId) && r.IsActive && !r.IsDeleted);
        if (tenantCategoryId.HasValue)
            query = query.Where(r => r.TenantCategoryId == tenantCategoryId.Value);

        var rows = await query
            .OrderBy(r => r.TenantCategory.Order)
            .ThenBy(r => r.ChargeType.SortOrder)
            .Select(r => new
            {
                r.SpaceId,
                CategoryName = r.TenantCategory.Name,
                ChargeTypeName = r.ChargeType.Name,
                r.CalculationMethod,
                r.UnitValue,
                r.KdvRate
            })
            .ToListAsync();

        return rows.Select(r => (r.SpaceId, new KiraTakip.Models.Dtos.RateHierarchy.ParentRateRowDto(
            r.CategoryName, r.ChargeTypeName, r.CalculationMethod, r.UnitValue, r.KdvRate, null))).ToList();
    }

    public Task<SpaceRate?> GetByKeyAsync(int spaceId, int tenantCategoryId, int chargeTypeId)
        => _dbSet.FirstOrDefaultAsync(r => r.SpaceId == spaceId
                                           && r.TenantCategoryId == tenantCategoryId
                                           && r.ChargeTypeId == chargeTypeId);
}
