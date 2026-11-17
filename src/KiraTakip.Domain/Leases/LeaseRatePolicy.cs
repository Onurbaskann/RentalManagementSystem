using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Leases;

public static class LeaseRatePolicy
{
    public static bool CanUseCalculationMethod(
        decimal unitArea,
        CalculationMethod calculationMethod)
        => calculationMethod != CalculationMethod.M2 || unitArea > 0;

    public static decimal CalculateMultiplier(
        CalculationMethod calculationMethod,
        decimal unitArea)
        => calculationMethod == CalculationMethod.M2 ? unitArea : 1m;

    public static decimal CalculateMultiplier(
        CalculationMethod calculationMethod,
        decimal unitArea,
        decimal? rentedArea,
        bool isLeaseRateSource)
    {
        if (calculationMethod == CalculationMethod.M2)
            return rentedArea ?? unitArea;

        if (isLeaseRateSource || rentedArea is null)
            return 1m;

        if (unitArea <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitArea));

        return rentedArea.Value / unitArea;
    }

    public static decimal CalculateBaseAmount(
        CalculationMethod calculationMethod,
        decimal unitValue,
        decimal unitArea)
        => unitValue * CalculateMultiplier(calculationMethod, unitArea);

    public static decimal CalculateBaseAmount(
        CalculationMethod calculationMethod,
        decimal unitValue,
        decimal unitArea,
        decimal? rentedArea,
        bool isLeaseRateSource)
        => Math.Round(unitValue * CalculateMultiplier(calculationMethod, unitArea, rentedArea, isLeaseRateSource), 2);
}
