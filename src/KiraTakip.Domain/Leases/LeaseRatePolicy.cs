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

    public static decimal CalculateBaseAmount(
        CalculationMethod calculationMethod,
        decimal unitValue,
        decimal unitArea)
        => unitValue * CalculateMultiplier(calculationMethod, unitArea);
}
