using KiraTakip.Domain.Charges;

namespace KiraTakip.Tests;

public class ChargePeriodPolicyTests
{
    [Fact]
    public void GetMonthlyPeriodStarts_ShouldIncludeEveryLeaseMonth()
    {
        var result = ChargePeriodPolicy.GetMonthlyPeriodStarts(
            new DateTime(2026, 1, 20),
            new DateTime(2026, 3, 5)).ToList();

        Assert.Equal(
            [
                new DateTime(2026, 1, 1),
                new DateTime(2026, 2, 1),
                new DateTime(2026, 3, 1)
            ],
            result);
    }

    [Fact]
    public void GetPeriodEnd_ShouldUseLeaseEndForFinalPartialMonth()
    {
        var result = ChargePeriodPolicy.GetPeriodEnd(
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 5));

        Assert.Equal(new DateTime(2026, 3, 5), result);
    }

    [Fact]
    public void GetPeriodEnd_ShouldUseMonthEndForEarlierPeriod()
    {
        var result = ChargePeriodPolicy.GetPeriodEnd(
            new DateTime(2026, 2, 1),
            new DateTime(2026, 3, 5));

        Assert.Equal(new DateTime(2026, 2, 28), result);
    }
}
