using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class StatisticsService(
    IChargeTypeRepository chargeTypeRepository,
    IRateResolverService rateResolver) : IStatisticsService
{
    public OccupancyStatus GetUnitStatus(Unit unit)
    {
        var activeLease = unit.Leases
            .Where(lease =>
                lease.Status == LeaseStatus.Active
                && lease.StartDate <= DateTime.Now
                && lease.EndDate >= DateTime.Now)
            .OrderByDescending(lease => lease.EndDate)
            .FirstOrDefault();

        if (activeLease == null) return OccupancyStatus.Vacant;

        var remainingDays = (activeLease.EndDate - DateTime.Now).Days;
        return remainingDays <= 30 ? OccupancyStatus.ExpiringSoon : OccupancyStatus.Leased;
    }

    public Lease? GetActiveLease(Unit unit)
        => unit.Leases
            .Where(lease =>
                lease.Status == LeaseStatus.Active
                && lease.StartDate <= DateTime.Now
                && lease.EndDate >= DateTime.Now)
            .OrderByDescending(lease => lease.EndDate)
            .FirstOrDefault();

    public bool IsActive(Lease lease)
        => lease.Status == LeaseStatus.Active
            && lease.StartDate <= DateTime.Now
            && lease.EndDate >= DateTime.Now;

    public async Task<decimal> GetMonthlyAmountAsync(Lease lease)
        => await GetMonthlyAmountAsync(
            lease.Id,
            lease.TenantId,
            lease.UnitId,
            lease.Unit?.Area ?? 0m,
            DateTime.Today);

    public async Task<LeaseSummaryDto> GetLeaseSummaryAsync(GetLeaseSummaryInput input)
    {
        var monthlyAmount = await GetMonthlyAmountAsync(
            input.LeaseId,
            input.TenantId,
            input.UnitId,
            input.UnitArea,
            input.CurrentTime.Date);
        var isActive = input.Status == LeaseStatus.Active
            && input.StartDate <= input.CurrentTime
            && input.EndDate >= input.CurrentTime;
        var remainingDays = (int)(input.EndDate - input.CurrentTime).TotalDays;
        var totalDays = (input.EndDate - input.StartDate).TotalDays;
        var elapsedDays = (input.CurrentTime - input.StartDate).TotalDays;
        var durationPercentage = totalDays <= 0
            ? 100
            : Math.Min(100, Math.Max(0, elapsedDays / totalDays * 100));
        var unitStatus = !isActive
            ? OccupancyStatus.Vacant
            : remainingDays <= 30
                ? OccupancyStatus.ExpiringSoon
                : OccupancyStatus.Leased;

        return new LeaseSummaryDto(
            remainingDays,
            monthlyAmount,
            monthlyAmount * 12,
            isActive,
            durationPercentage,
            unitStatus);
    }

    private async Task<decimal> GetMonthlyAmountAsync(
        int leaseId,
        int tenantId,
        int unitId,
        decimal area,
        DateTime period)
    {
        var allChargeTypes = await chargeTypeRepository.GetActiveGenerationTypesAsync();
        var chargeTypes = allChargeTypes
            .Where(chargeType => chargeType.Behavior == ChargeTypeBehavior.MonthlyFixed)
            .ToList();

        decimal total = 0m;
        foreach (var chargeType in chargeTypes)
        {
            var snapshot = await rateResolver.ResolveAsync(
                leaseId,
                tenantId,
                unitId,
                chargeType.Id,
                period);
            if (snapshot == null) continue;

            total += snapshot.CalculationMethod == CalculationMethod.M2
                ? snapshot.UnitValue * area
                : snapshot.UnitValue;
        }

        return total;
    }

    public async Task<decimal> GetAnnualAmountAsync(Lease lease)
        => await GetMonthlyAmountAsync(lease) * 12;

    public int GetRemainingDays(Lease lease)
        => (int)(lease.EndDate - DateTime.Now).TotalDays;

    public double GetDurationPercentage(Lease lease)
    {
        var total = (lease.EndDate - lease.StartDate).TotalDays;
        var elapsed = (DateTime.Now - lease.StartDate).TotalDays;
        if (total <= 0) return 100;

        return Math.Min(100, Math.Max(0, elapsed / total * 100));
    }

    public decimal CalculateInflationAdjustedAmount(CalculateInflationAdjustedAmountInput input)
    {
        Guard.Against(
            input.InflationRate < 0,
            "TÜFE oranı negatif olamaz.",
            "Lease.InvalidInflationRate");
        return input.CurrentAmount + (input.CurrentAmount * input.InflationRate / 100);
    }

    public decimal CalculateVatAmount(CalculateVatAmountInput input)
    {
        Guard.Against(
            input.VatRate < 0,
            "KDV oranı negatif olamaz.",
            "Lease.InvalidVatRate");
        return input.AmountExcludingVat * input.VatRate / 100;
    }

    public decimal CalculateVatIncludedAmount(CalculateVatIncludedAmountInput input)
        => input.AmountExcludingVat
            + CalculateVatAmount(new CalculateVatAmountInput(input.AmountExcludingVat, input.VatRate));

    public RentIncreaseCalculationResult CalculateRentIncrease(CalculateRentIncreaseInput input)
    {
        var result = new RentIncreaseCalculationResult
        {
            CurrentRentAmount = input.CurrentRentAmount,
            InflationRate = input.InflationRate,
            IsVatApplied = input.ApplyVat,
            VatRate = input.ApplyVat ? (input.VatRate ?? 20) : null
        };

        var inflationIncreaseAmount = input.InflationRate.HasValue
            ? input.CurrentRentAmount * input.InflationRate.Value / 100
            : 0;

        var rentAfterInflation = input.CurrentRentAmount + inflationIncreaseAmount;
        result.InflationIncreaseAmount = inflationIncreaseAmount;
        result.RentAfterInflation = rentAfterInflation;

        if (input.ApplyVat)
        {
            var rate = input.VatRate ?? 20;
            result.VatAmount = rentAfterInflation * rate / 100;
            result.TotalIncludingVat = rentAfterInflation + result.VatAmount;
        }
        else
        {
            result.VatAmount = 0;
            result.TotalIncludingVat = rentAfterInflation;
        }

        return result;
    }
}
