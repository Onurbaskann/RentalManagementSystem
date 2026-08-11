using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class LeaseRateOverrideRepository : RepositoryBase<LeaseRateOverride>, ILeaseRateOverrideRepository
{
    public LeaseRateOverrideRepository(ApplicationDbContext ctx) : base(ctx) { }

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
