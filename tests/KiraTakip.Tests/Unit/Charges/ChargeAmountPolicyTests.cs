using KiraTakip.Domain.Charges;

namespace KiraTakip.Tests;

public class ChargeAmountPolicyTests
{
    [Fact]
    public void Calculate_ShouldApplyMultiplierVatAndMonetaryRounding()
    {
        var result = ChargeAmountPolicy.Calculate(
            baseAmount: 100.005m,
            multiplier: 1.5m,
            vatRate: 20m);

        Assert.Equal(150.01m, result.Amount);
        Assert.Equal(30.00m, result.VatAmount);
        Assert.Equal(180.01m, result.TotalAmount);
    }

    [Fact]
    public void CalculateLineItemMultiplier_ShouldRoundToSixDecimalPlaces()
    {
        var result = ChargeAmountPolicy.CalculateLineItemMultiplier(
            3.14159265m,
            0.33333333m);

        Assert.Equal(1.047198m, result);
    }
}
