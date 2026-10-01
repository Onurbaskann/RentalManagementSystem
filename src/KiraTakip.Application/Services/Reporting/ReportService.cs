using KiraTakip.Models.Dtos.Report;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Reporting;

namespace KiraTakip.Services.Reporting;

public class ReportService(
    IChargeRepository chargeRepository,
    IPaymentAllocationRepository paymentAllocationRepository,
    IStoreRepository storeRepository,
    IChargeTypeRepository chargeTypeRepository) : IReportService
{
    public async Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input)
    {
        var report = await chargeRepository.GetMonthlyCollectionReportAsync(input);
        var collectedRows = await paymentAllocationRepository.GetCollectedByMonthAsync(input);
        var collectedByMonth = collectedRows.ToDictionary(row => row.Month);

        var rowsByMonth = report.Rows.ToDictionary(row => row.Month);

        report.Rows = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var row = rowsByMonth.GetValueOrDefault(month)
                    ?? new MonthlyCollectionReportRowDto { Month = month };
                if (collectedByMonth.TryGetValue(month, out var collected))
                {
                    row.CollectedAmount = collected.CollectedAmount;
                    row.CollectedPaymentCount = collected.CollectedPaymentCount;
                }
                return row;
            })
            .ToList();

        if (!report.AvailableYears.Contains(input.Year))
        {
            report.AvailableYears.Add(input.Year);
            report.AvailableYears = report.AvailableYears
                .OrderByDescending(year => year)
                .ToList();
        }

        var stores = await storeRepository.GetAllOptionsAsync();
        var chargeTypes = await chargeTypeRepository.GetListAsync();

        report.Stores = stores;
        report.ChargeTypes = chargeTypes
            .OrderBy(chargeType => chargeType.Name)
            .Select(chargeType => new ReportChargeTypeOptionDto(chargeType.Id, chargeType.Name))
            .ToList();
        report.SelectedStoreId = input.StoreId;
        report.SelectedChargeTypeId = input.ChargeTypeId;

        report.OdenenByPaymentYear = await paymentAllocationRepository.GetPaymentYearBreakdownForChargeYearAsync(input);
        report.CashBasisByChargeYear = await paymentAllocationRepository.GetCashBasisByChargeYearAsync(input);

        var priorYearsOverdue = await chargeRepository.GetPriorYearsOverdueAsync(input);
        report.PriorYearsOverdueCount = priorYearsOverdue.OverdueChargeCount;
        report.PriorYearsOverdueAmount = priorYearsOverdue.OverdueAmount;

        return report;
    }
}
