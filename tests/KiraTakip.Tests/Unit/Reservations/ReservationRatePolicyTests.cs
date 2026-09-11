using KiraTakip.Domain.Reservations;
using Xunit;

namespace KiraTakip.Tests;

public class ReservationRatePolicyTests
{
    [Theory]
    [InlineData(0, 60, 100, 20)]
    [InlineData(120, 1, 0, 0)]
    [InlineData(60, 60, 50.5, 100)]
    public void IsValid_WhenAllValuesValid_ReturnsTrue(
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal kdvRate)
    {
        var result = ReservationRatePolicy.IsValid(
            freeDurationMinutes,
            billingPeriodMinutes,
            periodRate,
            kdvRate);

        Assert.True(result);
    }

    [Theory]
    [InlineData(-1, 60, 100, 20)]
    [InlineData(-100, 60, 100, 20)]
    public void IsValid_WhenFreeDurationMinutesNegative_ReturnsFalse(
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal kdvRate)
    {
        var result = ReservationRatePolicy.IsValid(
            freeDurationMinutes,
            billingPeriodMinutes,
            periodRate,
            kdvRate);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, 0, 100, 20)]
    [InlineData(0, -1, 100, 20)]
    [InlineData(0, -60, 100, 20)]
    public void IsValid_WhenBillingPeriodMinutesZeroOrNegative_ReturnsFalse(
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal kdvRate)
    {
        var result = ReservationRatePolicy.IsValid(
            freeDurationMinutes,
            billingPeriodMinutes,
            periodRate,
            kdvRate);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, 60, -0.01, 20)]
    [InlineData(0, 60, -1, 20)]
    [InlineData(0, 60, -100, 20)]
    public void IsValid_WhenPeriodRateNegative_ReturnsFalse(
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal kdvRate)
    {
        var result = ReservationRatePolicy.IsValid(
            freeDurationMinutes,
            billingPeriodMinutes,
            periodRate,
            kdvRate);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, 60, 100, -0.01)]
    [InlineData(0, 60, 100, -1)]
    [InlineData(0, 60, 100, 100.01)]
    [InlineData(0, 60, 100, 101)]
    public void IsValid_WhenKdvRateOutOfRange_ReturnsFalse(
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal kdvRate)
    {
        var result = ReservationRatePolicy.IsValid(
            freeDurationMinutes,
            billingPeriodMinutes,
            periodRate,
            kdvRate);

        Assert.False(result);
    }
}
