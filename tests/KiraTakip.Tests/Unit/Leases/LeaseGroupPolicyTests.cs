using KiraTakip.Domain.Leases;

namespace KiraTakip.Tests;

public class LeaseGroupPolicyTests
{
    private static readonly DateTime Start = new(2026, 1, 1);
    private static readonly DateTime End = new(2026, 12, 31);

    [Fact]
    public void GetViolations_ShouldBeEmpty_ForValidGroup()
    {
        var shares = new[]
        {
            new LeaseGroupShare(1, 30m, Start, End),
            new LeaseGroupShare(2, 30m, Start, End)
        };

        Assert.Empty(LeaseGroupPolicy.GetViolations(60m, shares));
    }

    [Fact]
    public void GetViolations_ShouldReturnTooFewLeases_WhenSingleShare()
    {
        var shares = new[] { new LeaseGroupShare(1, 60m, Start, End) };

        Assert.Equal([LeaseGroupViolation.TooFewLeases], LeaseGroupPolicy.GetViolations(60m, shares));
    }

    [Fact]
    public void GetViolations_ShouldReturnDuplicateTenant_WhenSameTenantTwice()
    {
        var shares = new[]
        {
            new LeaseGroupShare(1, 30m, Start, End),
            new LeaseGroupShare(1, 30m, Start, End)
        };

        Assert.Equal([LeaseGroupViolation.DuplicateTenant], LeaseGroupPolicy.GetViolations(60m, shares));
    }

    [Fact]
    public void GetViolations_ShouldReturnAreaSumMismatch_WhenSumDiffers()
    {
        var shares = new[]
        {
            new LeaseGroupShare(1, 30m, Start, End),
            new LeaseGroupShare(2, 25m, Start, End)
        };

        Assert.Equal([LeaseGroupViolation.AreaSumMismatch], LeaseGroupPolicy.GetViolations(60m, shares));
    }

    [Fact]
    public void GetViolations_ShouldReturnDatesNotShared_WhenEndDatesDiffer()
    {
        var shares = new[]
        {
            new LeaseGroupShare(1, 30m, Start, End),
            new LeaseGroupShare(2, 30m, Start, End.AddDays(-1))
        };

        Assert.Equal([LeaseGroupViolation.DatesNotShared], LeaseGroupPolicy.GetViolations(60m, shares));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IsValidRestructureStart_ShouldRequireDayAfterTermination(int offsetDays, bool expected)
    {
        var termination = new DateTime(2026, 6, 10);

        Assert.Equal(
            expected,
            LeaseGroupPolicy.IsValidRestructureStart(termination, termination.AddDays(offsetDays)));
    }

    [Fact]
    public void SuggestRestructureStart_ShouldBeNextDay()
        => Assert.Equal(
            new DateTime(2026, 6, 11),
            LeaseGroupPolicy.SuggestRestructureStart(new DateTime(2026, 6, 10, 15, 0, 0)));
}
