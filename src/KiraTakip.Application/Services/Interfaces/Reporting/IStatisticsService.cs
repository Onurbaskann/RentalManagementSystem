using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Services.Interfaces.Reporting;

public interface IStatisticsService
{
    OccupancyStatus GetUnitStatus(Unit unit);
    Lease? GetActiveLease(Unit unit);
    bool IsActive(Lease lease);
    Task<decimal> GetMonthlyAmountAsync(Lease lease);
    // Toplu hali — GetMonthlyAmountAsync(Lease) ile aynı hesaplama, çok sayıda sözleşme için N+1
    // sorgu yerine tek seferde bulk-resolve eder (ör. Property/Details). Anahtar: Lease.Id.
    Task<Dictionary<int, decimal>> GetMonthlyAmountsAsync(IReadOnlyCollection<Lease> leases);
    Task<decimal> GetAnnualAmountAsync(Lease lease);
    Task<LeaseSummaryDto> GetLeaseSummaryAsync(GetLeaseSummaryInput input);
    int GetRemainingDays(Lease lease);
    double GetDurationPercentage(Lease lease);
    decimal CalculateInflationAdjustedAmount(CalculateInflationAdjustedAmountInput input);
    decimal CalculateVatAmount(CalculateVatAmountInput input);
    decimal CalculateVatIncludedAmount(CalculateVatIncludedAmountInput input);
    RentIncreaseCalculationResult CalculateRentIncrease(CalculateRentIncreaseInput input);
}
