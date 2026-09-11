namespace KiraTakip.Domain.Charges;

public static class ChargePeriodPolicy
{
    public static IEnumerable<DateTime> GetMonthlyPeriodStarts(
        DateTime leaseStartDate,
        DateTime leaseEndDate)
    {
        var periodStart = new DateTime(leaseStartDate.Year, leaseStartDate.Month, 1);
        var lastPeriodStart = new DateTime(leaseEndDate.Year, leaseEndDate.Month, 1);

        while (periodStart <= lastPeriodStart)
        {
            yield return periodStart;
            periodStart = periodStart.AddMonths(1);
        }
    }

    public static DateTime GetPeriodEnd(DateTime periodStartDate, DateTime leaseEndDate)
    {
        var monthEnd = periodStartDate.AddMonths(1).AddDays(-1);
        return leaseEndDate < monthEnd ? leaseEndDate : monthEnd;
    }
}
