using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PropertyRateOverrideRepository : BaseRepository<PropertyRateOverride>, IPropertyRateOverrideRepository
{
    public PropertyRateOverrideRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<PropertyRateOverride>> GetByPropertyIdAsync(int propertyId)
        => await _dbSet
            .AsNoTracking()
            .Where(f => f.PropertyId == propertyId)
            .ToListAsync();

    public async Task<PropertyPricingContextDto> GetPricingContextAsync(int propertyId)
    {
        var propertyName = propertyId == 0
            ? "Yeni Taşınmaz"
            : await _ctx.Properties.AsNoTracking()
                .Where(property => property.Id == propertyId)
                .Select(property => property.Name)
                .FirstOrDefaultAsync();

        var categories = await _ctx.Kategoriler.AsNoTracking()
            .Where(category => category.Type == CategoryType.Tenant)
            .OrderBy(category => category.Name)
            .Select(category => new PropertyPricingCategoryDto(category.Id, category.Name))
            .ToListAsync();
        var chargeTypes = await _ctx.ChargeTypes.AsNoTracking()
            .Where(type => type.Behavior != ChargeTypeBehavior.UserManual
                && type.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(type => type.SortOrder)
            .Select(type => new PropertyPricingChargeTypeDto(
                type.Id,
                type.Name,
                type.Code,
                type.Behavior))
            .ToListAsync();
        var rates = propertyId == 0
            ? []
            : await _dbSet.AsNoTracking()
                .Where(rate => rate.PropertyId == propertyId)
                .Select(rate => new PropertyPricingRateDto(
                    rate.Id,
                    rate.PropertyId,
                    rate.TenantCategoryId,
                    rate.ChargeTypeId,
                    rate.UnitValue,
                    rate.CalculationMethod,
                    rate.KdvRate))
                .ToListAsync();

        return new PropertyPricingContextDto(
            propertyId == 0 || propertyName != null,
            propertyName ?? string.Empty,
            categories,
            chargeTypes,
            rates);
    }

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
