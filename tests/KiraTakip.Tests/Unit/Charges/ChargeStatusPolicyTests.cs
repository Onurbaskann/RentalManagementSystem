using KiraTakip.Domain.Charges;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class ChargeStatusPolicyTests
{
    [Theory]
    [InlineData(1000, 1000, "2026-03-10", "2026-03-05", ChargeStatus.Paid)]
    [InlineData(1200, 1000, "2026-03-10", "2026-03-15", ChargeStatus.Paid)]
    [InlineData(500, 1000, "2026-03-10", "2026-03-05", ChargeStatus.PartiallyPaid)]
    [InlineData(500, 1000, "2026-03-10", "2026-03-15", ChargeStatus.PartiallyPaid)]
    [InlineData(0, 1000, "2026-03-10", "2026-03-15", ChargeStatus.Overdue)]
    [InlineData(0, 1000, "2026-03-10", "2026-03-10", ChargeStatus.Pending)]
    [InlineData(0, 1000, "2026-03-10", "2026-03-05", ChargeStatus.Pending)]
    public void DetermineStatus_ShouldReturnExpectedStatus(
        decimal paidAmount,
        decimal totalAmount,
        string dueDateStr,
        string todayStr,
        ChargeStatus expected)
    {
        var dueDate = DateTime.Parse(dueDateStr);
        var today = DateTime.Parse(todayStr);

        var result = ChargeStatusPolicy.DetermineStatus(paidAmount, totalAmount, dueDate, today);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-03-10", "2026-03-11", true)]
    [InlineData("2026-03-10", "2026-03-10", false)]
    [InlineData("2026-03-10", "2026-03-09", false)]
    public void IsOverdue_ShouldReturnExpected(
        string dueDateStr,
        string todayStr,
        bool expected)
    {
        var dueDate = DateTime.Parse(dueDateStr);
        var today = DateTime.Parse(todayStr);

        var result = ChargeStatusPolicy.IsOverdue(dueDate, today);

        Assert.Equal(expected, result);
    }
}
