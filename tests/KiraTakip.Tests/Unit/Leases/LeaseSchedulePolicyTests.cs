using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class LeaseSchedulePolicyTests
{
    [Fact]
    public void HasValidDateRange_ShouldRequireEndDateAfterStartDate()
    {
        var startDate = new DateTime(2026, 1, 1);

        Assert.True(LeaseSchedulePolicy.HasValidDateRange(startDate, startDate.AddDays(1)));
        Assert.False(LeaseSchedulePolicy.HasValidDateRange(startDate, startDate));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(31, true)]
    [InlineData(32, false)]
    public void IsValidDueDay_ShouldAcceptOnlyCalendarDayRange(int dueDay, bool expected)
        => Assert.Equal(expected, LeaseSchedulePolicy.IsValidDueDay(dueDay));

    [Fact]
    public void CanExtend_ShouldRequireNewerEndDate()
    {
        var currentEndDate = new DateTime(2026, 12, 31);

        Assert.True(LeaseSchedulePolicy.CanExtend(currentEndDate, currentEndDate.AddDays(1)));
        Assert.False(LeaseSchedulePolicy.CanExtend(currentEndDate, currentEndDate));
    }

    [Theory]
    [InlineData(LeaseStatus.Active, "2026-01-01", "2026-12-31", "2026-06-01", true)]
    [InlineData(LeaseStatus.Active, "2026-01-01", "2026-12-31", "2025-12-31", false)]
    [InlineData(LeaseStatus.Active, "2026-01-01", "2026-12-31", "2027-01-01", false)]
    [InlineData(LeaseStatus.Ended, "2026-01-01", "2026-12-31", "2026-06-01", false)]
    [InlineData(LeaseStatus.Terminated, "2026-01-01", "2026-12-31", "2026-06-01", false)]
    [InlineData(LeaseStatus.Draft, "2026-01-01", "2026-12-31", "2026-06-01", false)]
    public void IsActive_ShouldEnforceStatusAndDateBounds(
        LeaseStatus status,
        string start,
        string end,
        string current,
        bool expected)
    {
        var startDate = DateTime.Parse(start);
        var endDate = DateTime.Parse(end);
        var currentTime = DateTime.Parse(current);

        Assert.Equal(expected, LeaseSchedulePolicy.IsActive(status, startDate, endDate, currentTime));
    }

    [Fact]
    public void GetRemainingDays_ShouldReturnDifferenceInDays()
    {
        var endDate = new DateTime(2026, 12, 31);
        var currentTime = new DateTime(2026, 12, 21);

        Assert.Equal(10, LeaseSchedulePolicy.GetRemainingDays(endDate, currentTime));
    }

    [Fact]
    public void GetDurationPercentage_ShouldHandleBoundsAndRatios()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 11); // 10 days

        Assert.Equal(50, LeaseSchedulePolicy.GetDurationPercentage(start, end, new DateTime(2026, 1, 6)));
        Assert.Equal(0, LeaseSchedulePolicy.GetDurationPercentage(start, end, new DateTime(2025, 12, 31)));
        Assert.Equal(100, LeaseSchedulePolicy.GetDurationPercentage(start, end, new DateTime(2026, 1, 15)));
        Assert.Equal(100, LeaseSchedulePolicy.GetDurationPercentage(start, start, new DateTime(2026, 1, 1)));
    }
}
