namespace KiraTakip.Domain.Charges;

public static class ChargeReminderSchedulePolicy
{
    public static DateTime CalculateDueDateLimit(DateTime today, int daysBefore)
        => today.AddDays(daysBefore);

    public static DateTime CalculateCooldownThreshold(DateTime today, int cooldownDays)
        => today.AddDays(-cooldownDays);

    public static bool IsEligibleForReminder(DateTime? lastReminderDate, DateTime cooldownThreshold)
        => lastReminderDate == null || lastReminderDate.Value.Date <= cooldownThreshold.Date;
}
