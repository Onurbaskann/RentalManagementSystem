using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class RateScheduleRepository : BaseRepository<RateSchedule>, IRateScheduleRepository
{
    public RateScheduleRepository(ApplicationDbContext ctx) : base(ctx) { }

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

    public async Task<List<ParentTarifeSatir>> GetByYilKategoriForKartAsync(int yil, int? kategoriId)
    {
        var q = _dbSet.AsNoTracking()
            .Where(k => k.Year == yil && k.IsActive
                     && k.ChargeType.Behavior != ChargeTypeBehavior.UserManual
                     && k.ChargeType.Behavior != ChargeTypeBehavior.ReservationSpecific);
        if (kategoriId.HasValue)
            q = q.Where(k => k.TenantCategoryId == kategoriId.Value);

        return await q
            .OrderBy(k => k.TenantCategory.Order)
            .ThenBy(k => k.ChargeType.SortOrder)
            .Select(k => new ParentTarifeSatir
            {
                CategoryName = k.TenantCategory.Name,
                ChargeTypeName = k.ChargeType.Name,
                CalculationMethod = k.CalculationMethod,
                UnitValue = k.UnitValue,
                KdvRate = k.KdvRate
            })
            .ToListAsync();
    }
}
