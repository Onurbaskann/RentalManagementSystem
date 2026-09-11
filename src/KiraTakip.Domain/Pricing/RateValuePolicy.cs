using KiraTakip.Domain.Financial;
using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Pricing;

public static class RateValuePolicy
{
    public static bool IsValid(decimal unitValue,
                               decimal vatRate,
                               CalculationMethod calculationMethod,
                               ChargeTypeBehavior chargeTypeBehavior)
    {
        return unitValue >= 0
                && VatPolicy.IsValidRate(vatRate)
                && calculationMethod is CalculationMethod.Fixed or CalculationMethod.M2
                && (chargeTypeBehavior != ChargeTypeBehavior.FirstMonthOneTime
                    || calculationMethod == CalculationMethod.Fixed);
    }

    public static bool IsValid(decimal? unitValue,
                               decimal? vatRate,
                               CalculationMethod calculationMethod)
    {
        return (unitValue == null || unitValue >= 0)
                && (vatRate == null || VatPolicy.IsValidRate(vatRate.Value))
                && Enum.IsDefined(calculationMethod);
    }
}
