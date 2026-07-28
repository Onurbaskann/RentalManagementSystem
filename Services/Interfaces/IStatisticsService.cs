using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IStatisticsService
{
    OccupancyStatus GetUnitStatus(Unit unit);
    Lease? GetActiveLease(Unit unit);
    bool IsActive(Lease lease);
    Task<decimal> GetMonthlyAmountAsync(Lease lease);
    Task<decimal> GetAnnualAmountAsync(Lease lease);
    Task<LeaseSummaryDto> GetLeaseSummaryAsync(GetLeaseSummaryInput input);
    int GetRemainingDays(Lease lease);
    double GetDurationPercentage(Lease lease);
    decimal CalculateInflationAdjustedAmount(CalculateInflationAdjustedAmountInput input);
    decimal CalculateVatAmount(CalculateVatAmountInput input);
    decimal CalculateVatIncludedAmount(CalculateVatIncludedAmountInput input);
    RentIncreaseCalculationResult CalculateRentIncrease(CalculateRentIncreaseInput input);
}
