using KiraTakip.Models.Dtos.Report;

namespace KiraTakip.Services.Interfaces.Reporting;

public interface IReportService
{
    Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input);
}
