using KiraTakip.Domain.Charges;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class ChargeProrationPolicyTests
{
    [Fact]
    public void CalculatePeriodMultiplier_ShouldReturnOneForFullMonth()
    {
        var result = ChargeProrationPolicy.CalculatePeriodMultiplier(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31));

        Assert.Equal(1m, result);
    }

    [Fact]
    public void CalculatePeriodMultiplier_ShouldUseInclusiveActiveDaysOverThirty()
    {
        var result = ChargeProrationPolicy.CalculatePeriodMultiplier(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 22),
            new DateTime(2026, 1, 31));

        Assert.Equal(10m / 30m, result);
    }

    [Fact]
    public void CalculatePeriodMultiplier_ShouldNotExceedOne()
    {
        var result = ChargeProrationPolicy.CalculatePeriodMultiplier(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 30));

        Assert.Equal(1m, result);
    }

    [Fact]
    public void ResolveLineItemMultiplier_ShouldExcludeFirstMonthOneTimeChargeFromProration()
    {
        var result = ChargeProrationPolicy.ResolveLineItemMultiplier(
            ChargeTypeBehavior.FirstMonthOneTime,
            0.5m);

        Assert.Equal(1m, result);
    }
}
