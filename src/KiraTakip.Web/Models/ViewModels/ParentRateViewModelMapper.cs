using KiraTakip.Models.Dtos.RateHierarchy;

namespace KiraTakip.Web.Models.ViewModels;

public static class ParentRateViewModelMapper
{
    public static ParentRateCardViewModel? ToViewModel(this ParentRateCardDto? data)
        => data == null
            ? null
            : new ParentRateCardViewModel
            {
                SourceName = data.SourceName,
                Description = data.Description,
                Rows = data.Rows.Select(row => new ParentRateRowViewModel
                {
                    CategoryName = row.CategoryName,
                    ChargeTypeName = row.ChargeTypeName,
                    CalculationMethod = row.CalculationMethod,
                    UnitValue = row.UnitValue,
                    VatRate = row.VatRate,
                    Source = row.Source
                }).ToList()
            };

    public static ParentReservationRateOverrideCardViewModel? ToViewModel(
        this ParentReservationRateOverrideCardDto? data)
        => data == null
            ? null
            : new ParentReservationRateOverrideCardViewModel
            {
                SourceName = data.SourceName,
                Description = data.Description,
                Rows = data.Rows.Select(row => new ParentReservationRateOverrideRow
                {
                    UnitTypeName = row.UnitTypeName,
                    FreeDurationMinutes = row.FreeDurationMinutes,
                    BillingPeriodMinutes = row.BillingPeriodMinutes,
                    PeriodRate = row.PeriodRate,
                    VatRate = row.VatRate
                }).ToList()
            };
}
