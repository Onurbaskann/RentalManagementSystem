using KiraTakip.Domain.Financial;

namespace KiraTakip.Tests;

public class VatPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(18, true)]
    [InlineData(20, true)]
    [InlineData(100, true)]
    [InlineData(-0.01, false)]
    [InlineData(-1, false)]
    [InlineData(100.01, false)]
    [InlineData(101, false)]
    public void IsValidRate_ShouldValidateBoundaryConditions(decimal vatRate, bool expected)
    {
        var result = VatPolicy.IsValidRate(vatRate);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1000, 20, 200.00)]
    [InlineData(1000, 0, 0.00)]
    [InlineData(1000, 100, 1000.00)]
    [InlineData(0, 20, 0.00)]
    [InlineData(123.456, 20, 24.69)]
    [InlineData(100.005, 20, 20.00)] // 20.001 -> 20.00
    [InlineData(12.5, 20, 2.50)]
    public void CalculateAmount_ShouldCalculateWithTwoDecimalsAndMidpointRoundingToEven(
        decimal amountExcludingVat,
        decimal vatRate,
        decimal expectedVatAmount)
    {
        var result = VatPolicy.CalculateAmount(amountExcludingVat, vatRate);
        Assert.Equal(expectedVatAmount, result);
    }

    [Theory]
    // 12.525: 12.525 * 20 / 100 = 2.505. Midpoint: last digit 0 is even, so rounds to 2.50
    [InlineData(12.525, 20, 2.50)]
    // 12.575: 12.575 * 20 / 100 = 2.515. Midpoint: last digit 1 is odd, so rounds to 2.52
    [InlineData(12.575, 20, 2.52)]
    public void CalculateAmount_ShouldUseMidpointRoundingToEvenExplicitly(
        decimal amountExcludingVat,
        decimal vatRate,
        decimal expectedVatAmount)
    {
        var result = VatPolicy.CalculateAmount(amountExcludingVat, vatRate);
        Assert.Equal(expectedVatAmount, result);
    }

    [Theory]
    [InlineData(1000, 20, 1200.00)]
    [InlineData(1000, 0, 1000.00)]
    [InlineData(1000, 100, 2000.00)]
    [InlineData(0, 20, 0.00)]
    [InlineData(123.456, 20, 148.146)] // 123.456 + 24.69 = 148.146
    public void CalculateIncludedAmount_ShouldAddBaseAmountAndRoundedVatAmount(
        decimal amountExcludingVat,
        decimal vatRate,
        decimal expectedTotal)
    {
        var result = VatPolicy.CalculateIncludedAmount(amountExcludingVat, vatRate);
        Assert.Equal(expectedTotal, result);
    }
}
