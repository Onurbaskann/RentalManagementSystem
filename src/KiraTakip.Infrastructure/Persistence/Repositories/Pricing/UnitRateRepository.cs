using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Pricing;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Pricing;

public class UnitRateRepository(ApplicationDbContext ctx) : RepositoryBase<UnitRate>(ctx), IUnitRateRepository
{
    public async Task<RateValueDto?> GetRateAsync(int unitId, int tenantCategoryId, int chargeTypeId)
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId == unitId
                     && r.TenantCategoryId == tenantCategoryId
                     && r.ChargeTypeId == chargeTypeId
                     && r.IsActive
                     && !r.IsDeleted)
            .Select(r => new RateValueDto
            {
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentRateRowDto>> GetRowsByUnitAsync(int unitId, int? tenantCategoryId)
    {
        var query = _dbSet.AsNoTracking()
            .Where(rate => rate.UnitId == unitId && rate.IsActive && !rate.IsDeleted);
        if (tenantCategoryId.HasValue)
            query = query.Where(rate => rate.TenantCategoryId == tenantCategoryId.Value);

        var rates = await query
            .OrderBy(r => r.TenantCategory.Order)
            .ThenBy(r => r.ChargeType.SortOrder)
            .Select(r => new ParentRateRowDto(
                r.TenantCategory.Name,
                r.ChargeType.Name,
                r.CalculationMethod,
                r.UnitValue,
                r.KdvRate,
                null))
            .ToListAsync();

        return rates;
    }

    public async Task<UnitPricingContextDto> GetPricingContextAsync(int unitId, int year)
    {
        var unit = await _ctx.Units.AsNoTracking()
            .Where(item => item.Id == unitId && !item.IsDeleted)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.PropertyId,
                PropertyName = item.Property.Name,
                UnitTypeUsage = item.UnitType.Usage,
                UnitTypeName = item.UnitType.Name
            })
            .FirstOrDefaultAsync();

        if (unit == null)
            return new UnitPricingContextDto(
                false,
                unitId,
                string.Empty,
                0,
                string.Empty,
                UnitTypeUsage.Rentable,
                null,
                [],
                [],
                [],
                [],
                []);

        var categories = await _ctx.Kategoriler.AsNoTracking()
            .Where(category => category.Type == CategoryType.Tenant
                && category.IsActive
                && !category.IsDeleted)
            .OrderBy(category => category.Name)
            .Select(category => new UnitPricingCategoryDto(category.Id, category.Name))
            .ToListAsync();
        var chargeTypes = await _ctx.ChargeTypes.AsNoTracking()
            .Where(chargeType => chargeType.IsActive
                && !chargeType.IsDeleted
                && chargeType.Behavior != ChargeTypeBehavior.UserManual
                && chargeType.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(chargeType => chargeType.SortOrder)
            .Select(chargeType => new UnitPricingChargeTypeDto(
                chargeType.Id,
                chargeType.Name,
                chargeType.Code,
                chargeType.Behavior))
            .ToListAsync();
        var rates = await _dbSet.AsNoTracking()
            .Where(rate => rate.UnitId == unitId && rate.IsActive && !rate.IsDeleted)
            .Select(rate => new UnitPricingRateDto(
                rate.Id,
                rate.TenantCategoryId,
                rate.ChargeTypeId,
                rate.CalculationMethod,
                rate.UnitValue,
                rate.KdvRate))
            .ToListAsync();
        var propertyRates = await _ctx.TasinmazTarifeler.AsNoTracking()
            .Where(rate => rate.PropertyId == unit.PropertyId && rate.IsActive && !rate.IsDeleted)
            .Select(rate => new UnitPricingParentRateDto(
                rate.TenantCategoryId,
                rate.ChargeTypeId,
                rate.CalculationMethod,
                rate.UnitValue,
                rate.KdvRate))
            .ToListAsync();
        var generalRates = await _ctx.GenelTarifeler.AsNoTracking()
            .Where(rate => rate.Year == year && rate.IsActive && !rate.IsDeleted)
            .Select(rate => new UnitPricingParentRateDto(
                rate.TenantCategoryId,
                rate.ChargeTypeId,
                rate.CalculationMethod,
                rate.UnitValue,
                rate.KdvRate))
            .ToListAsync();

        return new UnitPricingContextDto(
            true,
            unit.Id,
            unit.Name,
            unit.PropertyId,
            unit.PropertyName,
            unit.UnitTypeUsage,
            unit.UnitTypeName,
            categories,
            chargeTypes,
            rates,
            propertyRates,
            generalRates);
    }

    public Task<List<UnitRate>> GetForUpdateAsync(int unitId)
        => _dbSet.IgnoreQueryFilters()
            .Where(rate => rate.UnitId == unitId)
            .ToListAsync();
}
