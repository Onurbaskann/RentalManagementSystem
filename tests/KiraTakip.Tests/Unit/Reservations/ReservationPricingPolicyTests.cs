using KiraTakip.Domain.Reservations;

namespace KiraTakip.Tests;

public class ReservationPricingPolicyTests
{
    [Fact]
    public void Calculate_ShouldRoundDurationAndPaidPeriodsUp()
    {
        var startDate = new DateTime(2026, 1, 1, 9, 0, 0);

        var result = ReservationPricingPolicy.Calculate(
            startDate,
            startDate.AddMinutes(90).AddSeconds(1),
            freeDurationMinutes: 30,
            billingPeriodMinutes: 60,
            periodRate: 100m,
            vatRate: 20m);

        Assert.Equal(91, result.TotalDurationMinutes);
        Assert.Equal(30, result.FreeDurationMinutes);
        Assert.Equal(61, result.PaidDurationMinutes);
        Assert.Equal(2, result.PaidPeriodCount);
        Assert.Equal(200m, result.RateAmount);
        Assert.Equal(40m, result.VatAmount);
        Assert.Equal(240m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroCharge_WhenFreeDurationCoversReservation()
    {
        var startDate = new DateTime(2026, 1, 1, 9, 0, 0);

        var result = ReservationPricingPolicy.Calculate(
            startDate,
            startDate.AddMinutes(45),
            freeDurationMinutes: 60,
            billingPeriodMinutes: 30,
            periodRate: 75m,
            vatRate: 20m);

        Assert.Equal(45, result.FreeDurationMinutes);
        Assert.Equal(0, result.PaidDurationMinutes);
        Assert.Equal(0, result.PaidPeriodCount);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Theory]
    [InlineData(-10.5, false)]
    [InlineData(-0.01, false)]
    [InlineData(0, false)]
    [InlineData(0.01, true)]
    [InlineData(100, true)]
    public void HasChargeableAmount_ShouldReturnTrueOnlyForPositiveAmounts(decimal totalAmount, bool expected)
    {
        var result = ReservationPricingPolicy.HasChargeableAmount(totalAmount);

        Assert.Equal(expected, result);
    }
}
