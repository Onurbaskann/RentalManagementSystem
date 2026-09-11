using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class LeaseRatePolicyTests
{
    [Theory]
    [InlineData(0, CalculationMethod.M2, false)]
    [InlineData(-1, CalculationMethod.M2, false)]
    [InlineData(1, CalculationMethod.M2, true)]
    [InlineData(0, CalculationMethod.Fixed, true)]
    public void CanUseCalculationMethod_ShouldRequireAreaOnlyForM2(
        decimal unitArea,
        CalculationMethod calculationMethod,
        bool expected)
        => Assert.Equal(
            expected,
            LeaseRatePolicy.CanUseCalculationMethod(unitArea, calculationMethod));

    [Theory]
    [InlineData(CalculationMethod.M2, 50, 50)]
    [InlineData(CalculationMethod.Fixed, 50, 1)]
    public void CalculateMultiplier_ShouldReturnExpected(
        CalculationMethod calculationMethod,
        decimal unitArea,
        decimal expected)
    {
        var result = LeaseRatePolicy.CalculateMultiplier(calculationMethod, unitArea);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(CalculationMethod.M2, 100, 50, 5000)]
    [InlineData(CalculationMethod.Fixed, 100, 50, 100)]
    public void CalculateBaseAmount_ShouldReturnExpected(
        CalculationMethod calculationMethod,
        decimal unitValue,
        decimal unitArea,
        decimal expected)
    {
        var result = LeaseRatePolicy.CalculateBaseAmount(calculationMethod, unitValue, unitArea);
        Assert.Equal(expected, result);
    }
}
