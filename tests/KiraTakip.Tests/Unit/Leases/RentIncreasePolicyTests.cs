using KiraTakip.Domain.Leases;

namespace KiraTakip.Tests;

public class RentIncreasePolicyTests
{
    [Theory]
    [InlineData(1000, 10, 1100)]
    [InlineData(1000, 0, 1000)]
    [InlineData(500, 50, 750)]
    public void CalculateInflationAdjustedAmount_ShouldCalculateCorrectly(
        decimal currentAmount,
        decimal inflationRate,
        decimal expected)
        => Assert.Equal(expected, RentIncreasePolicy.CalculateInflationAdjustedAmount(currentAmount, inflationRate));

    [Theory]
    [InlineData(0, true)]
    [InlineData(20, true)]
    [InlineData(100, true)]
    [InlineData(-0.01, false)]
    [InlineData(-1, false)]
    [InlineData(100.01, false)]
    [InlineData(101, false)]
    public void IsValidVatRate_ShouldValidateBoundaryConditions(decimal vatRate, bool expected)
        => Assert.Equal(expected, RentIncreasePolicy.IsValidVatRate(vatRate));

    [Theory]
    [InlineData(1000, 20, 200)]
    [InlineData(1000, 0, 0)]
    [InlineData(500, 10, 50)]
    public void CalculateVatAmount_ShouldCalculateCorrectly(
        decimal amount,
        decimal vatRate,
        decimal expected)
        => Assert.Equal(expected, RentIncreasePolicy.CalculateVatAmount(amount, vatRate));

    [Theory]
    [InlineData(1000, 20, 1200)]
    [InlineData(1000, 0, 1000)]
    public void CalculateVatIncludedAmount_ShouldCalculateCorrectly(
        decimal amount,
        decimal vatRate,
        decimal expected)
        => Assert.Equal(expected, RentIncreasePolicy.CalculateVatIncludedAmount(amount, vatRate));

    [Fact]
    public void Calculate_WithInflationAndVat_ShouldCalculateAllFields()
    {
        var result = RentIncreasePolicy.Calculate(
            currentRentAmount: 1000m,
            inflationRate: 10m,
            applyVat: true,
            vatRate: 20m);

        Assert.Equal(1000m, result.CurrentRentAmount);
        Assert.Equal(10m, result.InflationRate);
        Assert.Equal(100m, result.InflationIncreaseAmount);
        Assert.Equal(1100m, result.RentAfterInflation);
        Assert.True(result.IsVatApplied);
        Assert.Equal(20m, result.VatRate);
        Assert.Equal(220m, result.VatAmount);
        Assert.Equal(1320m, result.TotalIncludingVat);
    }

    [Fact]
    public void Calculate_WithoutInflationAndDefaultVat_ShouldDefaultVatRateTo20()
    {
        var result = RentIncreasePolicy.Calculate(
            currentRentAmount: 1000m,
            inflationRate: null,
            applyVat: true,
            vatRate: null);

        Assert.Equal(1000m, result.CurrentRentAmount);
        Assert.Null(result.InflationRate);
        Assert.Equal(0m, result.InflationIncreaseAmount);
        Assert.Equal(1000m, result.RentAfterInflation);
        Assert.True(result.IsVatApplied);
        Assert.Equal(20m, result.VatRate);
        Assert.Equal(200m, result.VatAmount);
        Assert.Equal(1200m, result.TotalIncludingVat);
    }

    [Fact]
    public void Calculate_WithoutVat_ShouldSetVatFieldsToZeroOrNull()
    {
        var result = RentIncreasePolicy.Calculate(
            currentRentAmount: 1000m,
            inflationRate: 15m,
            applyVat: false,
            vatRate: 20m);

        Assert.Equal(150m, result.InflationIncreaseAmount);
        Assert.Equal(1150m, result.RentAfterInflation);
        Assert.False(result.IsVatApplied);
        Assert.Null(result.VatRate);
        Assert.Equal(0m, result.VatAmount);
        Assert.Equal(1150m, result.TotalIncludingVat);
    }
}
