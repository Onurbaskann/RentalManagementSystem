using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Models.Dtos.RateHierarchy;

namespace KiraTakip.Services.Pricing;

public class RateHierarchyService(
    IPropertyRateOverrideRepository propertyRateRepository,
    IUnitRateRepository unitRateRepository,
    IRateScheduleRepository rateScheduleRepository,
    IReservationRateOverrideRepository reservationRateRepository,
    IUnitRepository unitRepository) : IRateHierarchyService
{

    public async Task<ParentRateCardDto?> GetParentForAsync(GetParentRateInput input)
    {
        int targetYear = input.Year ?? DateTime.Now.Year;

        // Lease katmanı: önce UnitRate'e bak
        if (input.Layer == RateHierarchyLayer.Lease && input.UnitId.HasValue)
        {
            var unitRows = await unitRateRepository.GetRowsByUnitAsync(input.UnitId.Value, input.TenantCategoryId);
            if (unitRows.Count > 0)
                return new ParentRateCardDto(
                    "Birim Tarifesi",
                    null,
                    unitRows);

            if (!input.PropertyId.HasValue)
                input = input with { PropertyId = await unitRepository.GetPropertyIdAsync(input.UnitId.Value) };
        }

        // Unit veya Lease katmanı: TasinmazTarife'a bak
        if (input.Layer is RateHierarchyLayer.Unit or RateHierarchyLayer.Lease
            && input.PropertyId.HasValue)
        {
            var rates = await propertyRateRepository.GetForHiyerarsiAsync(
                input.PropertyId.Value,
                input.TenantCategoryId);

            if (rates.Count > 0)
                return new ParentRateCardDto(
                    "Taşınmaz Tarifesi",
                    null,
                    rates.Select(rate => new ParentRateRowDto(
                        rate.TenantCategory.Name,
                        rate.ChargeType.Name,
                        rate.CalculationMethod,
                        rate.UnitValue,
                        rate.KdvRate)).ToList());
        }

        // Her katman için sonuç: Genel Tarife
        var rows = await rateScheduleRepository.GetRowsByYearAndCategoryAsync(
            targetYear,
            input.TenantCategoryId);

        return new ParentRateCardDto(
            $"Genel Tarife - {targetYear}",
            null,
            rows);
    }

    public async Task<ParentReservationRateOverrideCardDto?> GetReservationParentAsync(
        GetParentReservationRateInput input)
    {
        int targetYear = input.Year ?? DateTime.Now.Year;

        var rows = await reservationRateRepository.GetGeneralRowsAsync(targetYear);

        return new ParentReservationRateOverrideCardDto(
            $"Rezervasyon Tarifesi - {targetYear}",
            null,
            rows);
    }
}
