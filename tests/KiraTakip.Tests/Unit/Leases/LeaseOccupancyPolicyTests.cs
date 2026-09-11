using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class LeaseOccupancyPolicyTests
{
    [Fact]
    public void DetermineStatus_ShouldReturnVacant_WhenNoActiveLease()
    {
        var status = LeaseOccupancyPolicy.DetermineStatus(
            hasActiveLease: false,
            remainingDays: 100,
            expiringSoonThresholdDays: 30);

        Assert.Equal(OccupancyStatus.Vacant, status);
    }

    [Theory]
    [InlineData(10, 30, OccupancyStatus.ExpiringSoon)]
    [InlineData(30, 30, OccupancyStatus.ExpiringSoon)]
    [InlineData(0, 30, OccupancyStatus.ExpiringSoon)]
    [InlineData(-5, 30, OccupancyStatus.ExpiringSoon)]
    [InlineData(31, 30, OccupancyStatus.Leased)]
    [InlineData(100, 30, OccupancyStatus.Leased)]
    public void DetermineStatus_ShouldReturnExpiringSoonOrLeased_BasedOnThreshold(
        int remainingDays,
        int thresholdDays,
        OccupancyStatus expected)
    {
        var status = LeaseOccupancyPolicy.DetermineStatus(
            hasActiveLease: true,
            remainingDays: remainingDays,
            expiringSoonThresholdDays: thresholdDays);

        Assert.Equal(expected, status);
    }
}
