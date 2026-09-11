using KiraTakip.Domain.Tenants;

namespace KiraTakip.Tests;

public class TenantPanelSchedulePolicyTests
{
    [Theory]
    [InlineData("2026-03-15", "2026-03-10", 5)]
    [InlineData("2026-03-10", "2026-03-10", 0)]
    [InlineData("2026-03-05", "2026-03-10", -5)]
    public void CalculateDueDayDifference_ShouldReturnDays(string dueDateStr, string todayStr, int expected)
    {
        var dueDate = DateTime.Parse(dueDateStr);
        var today = DateTime.Parse(todayStr);

        var result = TenantPanelSchedulePolicy.CalculateDueDayDifference(dueDate, today);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1, UpcomingChargeUrgency.Overdue)]
    [InlineData(-10, UpcomingChargeUrgency.Overdue)]
    [InlineData(0, UpcomingChargeUrgency.DueSoon)]
    [InlineData(7, UpcomingChargeUrgency.DueSoon)]
    [InlineData(8, UpcomingChargeUrgency.Normal)]
    [InlineData(30, UpcomingChargeUrgency.Normal)]
    public void DetermineUpcomingChargeUrgency_ByDayDiff_ShouldReturnExpected(
        int dayDifference,
        UpcomingChargeUrgency expected)
    {
        var result = TenantPanelSchedulePolicy.DetermineUpcomingChargeUrgency(dayDifference);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-03-05", "2026-03-10", UpcomingChargeUrgency.Overdue)]
    [InlineData("2026-03-12", "2026-03-10", UpcomingChargeUrgency.DueSoon)]
    [InlineData("2026-03-17", "2026-03-10", UpcomingChargeUrgency.DueSoon)]
    [InlineData("2026-03-18", "2026-03-10", UpcomingChargeUrgency.Normal)]
    public void DetermineUpcomingChargeUrgency_ByDates_ShouldReturnExpected(
        string dueDateStr,
        string todayStr,
        UpcomingChargeUrgency expected)
    {
        var dueDate = DateTime.Parse(dueDateStr);
        var today = DateTime.Parse(todayStr);

        var result = TenantPanelSchedulePolicy.DetermineUpcomingChargeUrgency(dueDate, today);

        Assert.Equal(expected, result);
    }
}
