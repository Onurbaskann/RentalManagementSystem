using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Charges;

public static class ChargeDueDatePolicy
{
    public static DateTime Calculate(
        DateTime periodStartDate,
        DueDateRuleType ruleType,
        int dueDay)
        => ruleType switch
        {
            DueDateRuleType.FixedDayOfMonth =>
                new DateTime(
                    periodStartDate.Year,
                    periodStartDate.Month,
                    Math.Clamp(
                        dueDay,
                        1,
                        DateTime.DaysInMonth(periodStartDate.Year, periodStartDate.Month))),
            DueDateRuleType.PeriodStartOffset =>
                periodStartDate.AddDays(Math.Max(dueDay - 1, 0)),
            _ => periodStartDate
        };
}
