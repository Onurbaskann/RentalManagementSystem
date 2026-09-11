using KiraTakip.Domain.Financial;

namespace KiraTakip.Domain.Reservations;

public static class ReservationPricingPolicy
{
    public static ReservationPriceCalculation Calculate(
        DateTime startDate,
        DateTime endDate,
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal vatRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(billingPeriodMinutes);

        var totalDurationMinutes = (int)Math.Ceiling((endDate - startDate).TotalMinutes);
        var paidDurationMinutes = Math.Max(0, totalDurationMinutes - freeDurationMinutes);
        var paidPeriodCount = paidDurationMinutes == 0
            ? 0
            : (int)Math.Ceiling((double)paidDurationMinutes / billingPeriodMinutes);
        var rateAmount = paidPeriodCount * periodRate;
        var vatAmount = VatPolicy.CalculateAmount(rateAmount, vatRate);

        return new ReservationPriceCalculation(
            totalDurationMinutes,
            Math.Min(freeDurationMinutes, totalDurationMinutes),
            paidDurationMinutes,
            paidPeriodCount,
            periodRate,
            rateAmount,
            vatRate,
            vatAmount,
            rateAmount + vatAmount);
    }

    public static bool HasChargeableAmount(decimal totalAmount)
        => totalAmount > 0;
}

public sealed record ReservationPriceCalculation(
    int TotalDurationMinutes,
    int FreeDurationMinutes,
    int PaidDurationMinutes,
    int PaidPeriodCount,
    decimal UnitRate,
    decimal RateAmount,
    decimal VatRate,
    decimal VatAmount,
    decimal TotalAmount);
