using System.Globalization;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public static class MonthlyCollectionReportViewModelMapper
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static MonthlyCollectionReportViewModel ToViewModel(
        this MonthlyCollectionReportDto dto)
        => new()
        {
            Year = dto.Year,
            AvailableYears = dto.AvailableYears,
            Rows = dto.Rows.Select(row => new MonthlyCollectionReportRowViewModel
            {
                Month = row.Month,
                MonthName = new DateTime(dto.Year, row.Month, 1)
                    .ToString("MMMM", TurkishCulture),
                ChargeCount = row.ChargeCount,
                ExpectedAmount = row.ExpectedAmount,
                CollectedAmount = row.CollectedAmount,
                OverdueChargeCount = row.OverdueChargeCount,
                OverdueAmount = row.OverdueAmount
            }).ToList()
        };
}
