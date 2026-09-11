using KiraTakip.Domain.Financial;

namespace KiraTakip.Domain.Charges;

public static class ChargeAmountPolicy
{
    public static ChargeAmountCalculation Calculate(
        decimal baseAmount,
        decimal multiplier,
        decimal vatRate)
    {
        var amount = Math.Round(baseAmount * multiplier, 2);
        var vatAmount = VatPolicy.CalculateAmount(amount, vatRate);

        return new ChargeAmountCalculation(
            amount,
            vatAmount,
            amount + vatAmount);
    }

    public static decimal CalculateLineItemMultiplier(
        decimal baseMultiplier,
        decimal adjustmentMultiplier)
        => Math.Round(baseMultiplier * adjustmentMultiplier, 6);
}

public sealed record ChargeAmountCalculation(
    decimal Amount,
    decimal VatAmount,
    decimal TotalAmount);
