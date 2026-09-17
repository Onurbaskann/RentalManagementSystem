using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Services.Interfaces.Reporting;
using KiraTakip.Domain.Leases;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Services.Reporting;

public class StatisticsService(
    IChargeTypeRepository chargeTypeRepository,
    IRateResolverService rateResolver,
    IOperationalPolicyProvider operationalPolicyProvider) : IStatisticsService
{
    public OccupancyStatus GetUnitStatus(Unit unit)
    {
        var activeLease = unit.Leases
            .Where(lease => LeaseSchedulePolicy.IsActive(lease.Status, lease.StartDate, lease.EndDate, DateTime.Now))
            .OrderByDescending(lease => lease.EndDate)
            .FirstOrDefault();

        var remainingDays = activeLease != null
            ? LeaseSchedulePolicy.GetRemainingDays(activeLease.EndDate, DateTime.Now)
            : 0;

        return LeaseOccupancyPolicy.DetermineStatus(
            activeLease != null,
            remainingDays,
            operationalPolicyProvider.Current.LeaseExpiringSoonStatusDays);
    }

    public Lease? GetActiveLease(Unit unit)
        => unit.Leases
            .Where(lease => LeaseSchedulePolicy.IsActive(lease.Status, lease.StartDate, lease.EndDate, DateTime.Now))
            .OrderByDescending(lease => lease.EndDate)
            .FirstOrDefault();

    public bool IsActive(Lease lease)
        => LeaseSchedulePolicy.IsActive(lease.Status, lease.StartDate, lease.EndDate, DateTime.Now);

    public async Task<decimal> GetMonthlyAmountAsync(Lease lease)
        => await GetMonthlyAmountAsync(
            lease.Id,
            lease.TenantId,
            lease.UnitId,
            lease.Unit?.Area ?? 0m,
            DateTime.Today,
            lease.IsRentFree);

    public async Task<LeaseSummaryDto> GetLeaseSummaryAsync(GetLeaseSummaryInput input)
    {
        var monthlyAmount = await GetMonthlyAmountAsync(
            input.LeaseId,
            input.TenantId,
            input.UnitId,
            input.UnitArea,
            input.CurrentTime.Date,
            input.IsRentFree);
        var isActive = LeaseSchedulePolicy.IsActive(input.Status, input.StartDate, input.EndDate, input.CurrentTime);
        var remainingDays = LeaseSchedulePolicy.GetRemainingDays(input.EndDate, input.CurrentTime);
        var durationPercentage = LeaseSchedulePolicy.GetDurationPercentage(input.StartDate, input.EndDate, input.CurrentTime);
        var unitStatus = LeaseOccupancyPolicy.DetermineStatus(
            isActive,
            remainingDays,
            operationalPolicyProvider.Current.LeaseExpiringSoonStatusDays);

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
        DateTime period,
        bool isRentFree)
    {
        var allChargeTypes = await chargeTypeRepository.GetActiveGenerationTypesAsync();
        var chargeTypes = allChargeTypes
            .Where(chargeType => chargeType.Behavior == ChargeTypeBehavior.MonthlyFixed
                && LeaseBillingPolicy.ShouldIncludeChargeType(isRentFree, chargeType.Code))
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

            total += LeaseRatePolicy.CalculateBaseAmount(
                snapshot.CalculationMethod,
                snapshot.UnitValue,
                area);
        }

        return total;
    }

    public async Task<decimal> GetAnnualAmountAsync(Lease lease)
        => await GetMonthlyAmountAsync(lease) * 12;

    public int GetRemainingDays(Lease lease)
        => LeaseSchedulePolicy.GetRemainingDays(lease.EndDate, DateTime.Now);

    public double GetDurationPercentage(Lease lease)
        => LeaseSchedulePolicy.GetDurationPercentage(lease.StartDate, lease.EndDate, DateTime.Now);

    public decimal CalculateInflationAdjustedAmount(CalculateInflationAdjustedAmountInput input)
    {
        Guard.Against(
            input.InflationRate < 0,
            "TÜFE oranı negatif olamaz.",
            "Lease.InvalidInflationRate");
        return RentIncreasePolicy.CalculateInflationAdjustedAmount(input.CurrentAmount, input.InflationRate);
    }

    public decimal CalculateVatAmount(CalculateVatAmountInput input)
    {
        Guard.Against(
            !RentIncreasePolicy.IsValidVatRate(input.VatRate),
            "KDV oranı 0-100 arasında olmalıdır.",
            "Lease.InvalidVatRate");
        return RentIncreasePolicy.CalculateVatAmount(input.AmountExcludingVat, input.VatRate);
    }

    public decimal CalculateVatIncludedAmount(CalculateVatIncludedAmountInput input)
    {
        Guard.Against(
            !RentIncreasePolicy.IsValidVatRate(input.VatRate),
            "KDV oranı 0-100 arasında olmalıdır.",
            "Lease.InvalidVatRate");
        return RentIncreasePolicy.CalculateVatIncludedAmount(input.AmountExcludingVat, input.VatRate);
    }

    public RentIncreaseCalculationResult CalculateRentIncrease(CalculateRentIncreaseInput input)
    {
        var calculation = RentIncreasePolicy.Calculate(
            input.CurrentRentAmount,
            input.InflationRate,
            input.ApplyVat,
            input.VatRate);

        return new RentIncreaseCalculationResult
        {
            CurrentRentAmount = calculation.CurrentRentAmount,
            InflationRate = calculation.InflationRate,
            InflationIncreaseAmount = calculation.InflationIncreaseAmount,
            RentAfterInflation = calculation.RentAfterInflation,
            IsVatApplied = calculation.IsVatApplied,
            VatRate = calculation.VatRate,
            VatAmount = calculation.VatAmount,
            TotalIncludingVat = calculation.TotalIncludingVat
        };
    }
}
