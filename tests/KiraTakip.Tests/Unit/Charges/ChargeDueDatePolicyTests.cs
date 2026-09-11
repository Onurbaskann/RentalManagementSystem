using KiraTakip.Domain.Charges;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class ChargeDueDatePolicyTests
{
    [Theory]
    [InlineData(2026, 2, 31, 28)]
    [InlineData(2026, 2, 0, 1)]
    [InlineData(2024, 2, 31, 29)]
    public void FixedDayOfMonth_ShouldClampDueDayToMonth(
        int year,
        int month,
        int dueDay,
        int expectedDay)
    {
        var result = ChargeDueDatePolicy.Calculate(
            new DateTime(year, month, 1),
            DueDateRuleType.FixedDayOfMonth,
            dueDay);

        Assert.Equal(new DateTime(year, month, expectedDay), result);
    }

    [Theory]
    [InlineData(10, 9)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    public void PeriodStartOffset_ShouldUseDueDayAsOneBasedOffset(
        int dueDay,
        int expectedOffsetDays)
    {
        var periodStart = new DateTime(2026, 1, 1);

        var result = ChargeDueDatePolicy.Calculate(
            periodStart,
            DueDateRuleType.PeriodStartOffset,
            dueDay);

        Assert.Equal(periodStart.AddDays(expectedOffsetDays), result);
    }
}
