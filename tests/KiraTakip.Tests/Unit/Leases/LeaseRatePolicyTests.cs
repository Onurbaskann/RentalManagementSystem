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

    [Fact]
    public void CalculateMultiplier_M2_ShouldUseRentedAreaOrUnitArea()
    {
        Assert.Equal(20m, LeaseRatePolicy.CalculateMultiplier(CalculationMethod.M2, 60m, 20m, false));
        Assert.Equal(60m, LeaseRatePolicy.CalculateMultiplier(CalculationMethod.M2, 60m, null, false));
    }

    [Fact]
    public void CalculateMultiplier_FixedUpperLayer_ShouldScaleByAreaRatio()
    {
        var third = LeaseRatePolicy.CalculateMultiplier(CalculationMethod.Fixed, 60m, 20m, false);
        var twoThirds = LeaseRatePolicy.CalculateMultiplier(CalculationMethod.Fixed, 60m, 40m, false);

        Assert.Equal(2000m, Math.Round(6000m * third, 2));
        Assert.Equal(4000m, Math.Round(6000m * twoThirds, 2));
    }

    [Fact]
    public void CalculateMultiplier_Fixed_ShouldReturnOne_ForLeaseRateSourceOrNullRentedArea()
    {
        Assert.Equal(1m, LeaseRatePolicy.CalculateMultiplier(CalculationMethod.Fixed, 60m, 20m, true));
        Assert.Equal(1m, LeaseRatePolicy.CalculateMultiplier(CalculationMethod.Fixed, 60m, null, false));
    }

    [Fact]
    public void CalculateMultiplier_Fixed_ShouldThrow_WhenUnitAreaNotPositive()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => LeaseRatePolicy.CalculateMultiplier(CalculationMethod.Fixed, 0m, 20m, false));

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
