using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Pricing;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Pricing;

public class LeaseRateOverrideRepository(
    ApplicationDbContext ctx) : RepositoryBase<LeaseRateOverride>(ctx), ILeaseRateOverrideRepository
{
    public async Task<RateValueDto?> GetRateAsync(int leaseId, int chargeTypeId)
        => await _dbSet.AsNoTracking()
            .Where(r => r.LeaseId == leaseId && r.ChargeTypeId == chargeTypeId)
            .Select(r => new RateValueDto
            {
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            })
            .FirstOrDefaultAsync();

    public async Task<List<(int LeaseId, int ChargeTypeId, RateValueDto Rate)>> GetRatesAsync(
        IReadOnlyCollection<int> leaseIds, IReadOnlyCollection<int> chargeTypeIds)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(r => leaseIds.Contains(r.LeaseId) && chargeTypeIds.Contains(r.ChargeTypeId))
            .Select(r => new { r.LeaseId, r.ChargeTypeId, r.CalculationMethod, r.UnitValue, r.KdvRate })
            .ToListAsync();

        return rows
            .Select(r => (r.LeaseId, r.ChargeTypeId, new RateValueDto
            {
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            }))
            .ToList();
    }

    public async Task ReplaceAsync(int leaseId, IReadOnlyCollection<LeaseRateOverride> rateOverrides)
    {
        var existingRates = await _dbSet.Where(rate => rate.LeaseId == leaseId).ToListAsync();
        _dbSet.RemoveRange(existingRates);
        await _dbSet.AddRangeAsync(rateOverrides);
    }

    public Task<List<LeaseRateOverride>> GetWithChargeTypeAsync(int leaseId)
        => _dbSet
            .Include(rate => rate.ChargeType)
            .Where(rate => rate.LeaseId == leaseId)
            .ToListAsync();

    public async Task SoftDeleteByLeaseIdAsync(int leaseId)
    {
        var rates = await _dbSet.Where(rate => rate.LeaseId == leaseId).ToListAsync();
        foreach (var rate in rates)
        {
            rate.IsDeleted = true;
            rate.IsActive = false;
        }
    }
}
