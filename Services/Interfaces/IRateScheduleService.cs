using KiraTakip.Models.Dtos.RateSchedule;

namespace KiraTakip.Services.Interfaces;

public interface IRateScheduleService
{
    Task<List<RateYearSummaryDto>> GetYearSummariesAsync();
    Task<List<int>> GetExistingYearsAsync();
    Task<RateMatrixDto?> GetMatrixAsync(int year);
    Task SaveMatrixAsync(int year, SaveRateMatrixInput input);
    Task CreateYearAsync(CreateRateYearInput input);
    Task<bool> ToggleStatusAsync(int year);
}
