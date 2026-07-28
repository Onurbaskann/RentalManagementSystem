using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateHierarchyService(
    IPropertyRateOverrideRepository propertyRateRepository,
    IUnitRateRepository unitRateRepository,
    IRateScheduleRepository rateScheduleRepository,
    IReservationRateOverrideRepository reservationRateRepository,
    IUnitRepository unitRepository) : IRateHierarchyService
{

    public async Task<ParentRateCardViewModel?> GetParentForAsync(GetParentRateInput input)
    {
        int targetYear = input.Year ?? DateTime.Now.Year;

        // Lease katmanı: önce UnitRate'e bak
        if (input.Layer == RateHierarchyLayer.Lease && input.UnitId.HasValue)
        {
            var cards = await unitRateRepository.GetCardsByUnitAsync(input.UnitId.Value, input.TenantCategoryId);
            if (cards.Count > 0)
                return cards[0];

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
                return new ParentRateCardViewModel
                {
                    SourceName = "Taşınmaz Tarifesi",
                    Rows = rates.Select(rate => new ParentRateRowViewModel
                    {
                        CategoryName = rate.TenantCategory.Name,
                        ChargeTypeName = rate.ChargeType.Name,
                        CalculationMethod = rate.CalculationMethod,
                        UnitValue = rate.UnitValue,
                        VatRate = rate.KdvRate
                    }).ToList()
                };
        }

        // Her katman için sonuç: Genel Tarife
        var rows = await rateScheduleRepository.GetRowsByYearAndCategoryAsync(
            targetYear,
            input.TenantCategoryId);

        return new ParentRateCardViewModel
        {
            SourceName = $"Genel Tarife - {targetYear}",
            Rows = rows
        };
    }

    public async Task<ParentReservationRateOverrideCardViewModel?> GetReservationParentAsync(
        GetParentReservationRateInput input)
    {
        int targetYear = input.Year ?? DateTime.Now.Year;

        var rows = await reservationRateRepository.GetGeneralRowsAsync(targetYear);

        return new ParentReservationRateOverrideCardViewModel
        {
            SourceName = $"Rezervasyon Tarifesi - {targetYear}",
            Rows = rows
        };
    }
}
