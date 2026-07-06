using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class SozlesmeTarifeRepository : BaseRepository<SozlesmeTarife>, ISozlesmeTarifeRepository
{
    public SozlesmeTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

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
}
