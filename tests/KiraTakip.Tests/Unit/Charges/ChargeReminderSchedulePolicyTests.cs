using KiraTakip.Domain.Charges;
using Xunit;

namespace KiraTakip.Tests;

public class ChargeReminderSchedulePolicyTests
{
    [Fact]
    public void CalculateDueDateLimit_AddsDaysBeforeToToday()
    {
        var today = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Local);
        const int daysBefore = 5;

        var result = ChargeReminderSchedulePolicy.CalculateDueDateLimit(today, daysBefore);

        Assert.Equal(new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Local), result);
    }

    [Fact]
    public void CalculateCooldownThreshold_SubtractsCooldownDaysFromToday()
    {
        var today = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Local);
        const int cooldownDays = 3;

        var result = ChargeReminderSchedulePolicy.CalculateCooldownThreshold(today, cooldownDays);

        Assert.Equal(new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Local), result);
    }

    [Fact]
    public void IsEligibleForReminder_WhenLastReminderDateIsNull_ReturnsTrue()
    {
        var cooldownThreshold = new DateTime(2026, 9, 5, 0, 0, 0);

        var result = ChargeReminderSchedulePolicy.IsEligibleForReminder(null, cooldownThreshold);

        Assert.True(result);
    }

    [Fact]
    public void IsEligibleForReminder_WhenLastReminderDateIsBeforeThreshold_ReturnsTrue()
    {
        var cooldownThreshold = new DateTime(2026, 9, 5, 0, 0, 0);
        var lastReminderDate = new DateTime(2026, 9, 4, 15, 30, 0);

        var result = ChargeReminderSchedulePolicy.IsEligibleForReminder(lastReminderDate, cooldownThreshold);

        Assert.True(result);
    }

    [Fact]
    public void IsEligibleForReminder_WhenLastReminderDateEqualsThresholdDate_ReturnsTrue()
    {
        var cooldownThreshold = new DateTime(2026, 9, 5, 0, 0, 0);
        var lastReminderDate = new DateTime(2026, 9, 5, 23, 59, 59);

        var result = ChargeReminderSchedulePolicy.IsEligibleForReminder(lastReminderDate, cooldownThreshold);

        Assert.True(result);
    }

    [Fact]
    public void IsEligibleForReminder_WhenLastReminderDateIsAfterThresholdDate_ReturnsFalse()
    {
        var cooldownThreshold = new DateTime(2026, 9, 5, 0, 0, 0);
        var lastReminderDate = new DateTime(2026, 9, 6, 0, 0, 0);

        var result = ChargeReminderSchedulePolicy.IsEligibleForReminder(lastReminderDate, cooldownThreshold);

        Assert.False(result);
    }
}
