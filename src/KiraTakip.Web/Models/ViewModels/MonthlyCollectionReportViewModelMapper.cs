using System.Globalization;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Report;

namespace KiraTakip.Web.Models.ViewModels;

public static class MonthlyCollectionReportViewModelMapper
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static MonthlyCollectionReportViewModel ToViewModel(
        this MonthlyCollectionReportDto dto)
        => new()
        {
            Year = dto.Year,
            AvailableYears = dto.AvailableYears,
            SelectedStoreId = dto.SelectedStoreId,
            SelectedChargeTypeId = dto.SelectedChargeTypeId,
            Stores = dto.Stores
                .Select(store => new ReportOptionViewModel(store.Id, store.Name))
                .ToList(),
            ChargeTypes = dto.ChargeTypes
                .Select(chargeType => new ReportOptionViewModel(chargeType.Id, chargeType.Name))
                .ToList(),
            Rows = dto.Rows.Select(row => new MonthlyCollectionReportRowViewModel
            {
                Month = row.Month,
                MonthName = new DateTime(dto.Year, row.Month, 1)
                    .ToString("MMMM", TurkishCulture),
                ChargeCount = row.ChargeCount,
                ExpectedAmount = row.ExpectedAmount,
                CollectedAmount = row.CollectedAmount,
                CollectedPaymentCount = row.CollectedPaymentCount,
                OverdueChargeCount = row.OverdueChargeCount,
                OverdueAmount = row.OverdueAmount
            }).ToList(),
            LeaseChargeCount = dto.LeaseChargeCount,
            ManualChargeCount = dto.ManualChargeCount,
            ReservationChargeCount = dto.ReservationChargeCount,
            OdenenByPaymentYear = dto.OdenenByPaymentYear
                .Select(item => new ReportYearCountViewModel(item.Year, item.Count))
                .ToList(),
            CashBasisByChargeYear = dto.CashBasisByChargeYear
                .Select(item => new ReportYearAmountViewModel(item.Year, item.Count, item.Amount))
                .ToList(),
            PriorYearsOverdueCount = dto.PriorYearsOverdueCount,
            PriorYearsOverdueAmount = dto.PriorYearsOverdueAmount
        };
}
