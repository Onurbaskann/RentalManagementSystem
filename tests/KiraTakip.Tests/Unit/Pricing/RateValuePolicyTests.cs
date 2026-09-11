using KiraTakip.Domain.Pricing;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class RateValuePolicyTests
{
    [Theory]
    [InlineData(0, 0, CalculationMethod.Fixed, ChargeTypeBehavior.MonthlyFixed, true)]
    [InlineData(-1, 0, CalculationMethod.Fixed, ChargeTypeBehavior.MonthlyFixed, false)]
    [InlineData(1, -1, CalculationMethod.Fixed, ChargeTypeBehavior.MonthlyFixed, false)]
    [InlineData(1, 101, CalculationMethod.Fixed, ChargeTypeBehavior.MonthlyFixed, false)]
    [InlineData(1, 20, CalculationMethod.M2, ChargeTypeBehavior.MonthlyFixed, true)]
    [InlineData(1, 20, CalculationMethod.M2, ChargeTypeBehavior.FirstMonthOneTime, false)]
    [InlineData(1, 20, CalculationMethod.Fixed, ChargeTypeBehavior.FirstMonthOneTime, true)]
    public void IsValid_ShouldEnforceRateValueAndChargeTypeCompatibility(
        decimal unitValue,
        decimal vatRate,
        CalculationMethod calculationMethod,
        ChargeTypeBehavior chargeTypeBehavior,
        bool expected)
        => Assert.Equal(
            expected,
            RateValuePolicy.IsValid(
                unitValue,
                vatRate,
                calculationMethod,
                chargeTypeBehavior));

    [Theory]
    [InlineData(null, null, CalculationMethod.Fixed, true)]
    [InlineData(0.0, null, CalculationMethod.Fixed, true)]
    [InlineData(100.0, 20.0, CalculationMethod.Fixed, true)]
    [InlineData(100.0, 20.0, CalculationMethod.M2, true)]
    [InlineData(-1.0, 20.0, CalculationMethod.Fixed, false)]
    [InlineData(10.0, -1.0, CalculationMethod.Fixed, false)]
    [InlineData(10.0, 101.0, CalculationMethod.Fixed, false)]
    [InlineData(null, 101.0, CalculationMethod.Fixed, false)]
    [InlineData(null, -1.0, CalculationMethod.Fixed, false)]
    [InlineData(10.0, 20.0, (CalculationMethod)99, false)]
    public void IsValid_NullableOverload_ShouldEnforceRules(
        double? unitValue,
        double? vatRate,
        CalculationMethod calculationMethod,
        bool expected)
        => Assert.Equal(
            expected,
            RateValuePolicy.IsValid(
                unitValue.HasValue ? (decimal)unitValue.Value : null,
                vatRate.HasValue ? (decimal)vatRate.Value : null,
                calculationMethod));
}
