using KiraTakip.Domain.Leases;
using KiraTakip.Models.Constants;

namespace KiraTakip.Tests;

public sealed class LeaseBillingPolicyTests
{
    [Fact]
    public void RentFreeLease_ShouldExcludeOnlyRentChargeType()
    {
        Assert.False(LeaseBillingPolicy.ShouldIncludeChargeType(true, BorcTipiConsts.Kira));
        Assert.True(LeaseBillingPolicy.ShouldIncludeChargeType(true, BorcTipiConsts.Depozito));
        Assert.True(LeaseBillingPolicy.ShouldIncludeChargeType(true, "AIDAT"));
    }

    [Fact]
    public void PaidLease_ShouldIncludeRentChargeType()
        => Assert.True(LeaseBillingPolicy.ShouldIncludeChargeType(false, BorcTipiConsts.Kira));

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(0.01, true)]
    public void ChargeableAmount_ShouldRequirePositiveTotal(decimal amount, bool expected)
        => Assert.Equal(expected, LeaseBillingPolicy.HasChargeableAmount(amount));
}
