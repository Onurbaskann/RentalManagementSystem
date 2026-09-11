using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Charges;

public static class ChargeProrationPolicy
{
    public static decimal CalculatePeriodMultiplier(
        DateTime periodStartDate,
        DateTime leaseStartDate,
        DateTime leaseEndDate)
    {
        var monthEnd = periodStartDate.AddMonths(1).AddDays(-1);
        var activeStart = leaseStartDate > periodStartDate ? leaseStartDate : periodStartDate;
        var activeEnd = leaseEndDate < monthEnd ? leaseEndDate : monthEnd;

        if (activeStart == periodStartDate && activeEnd == monthEnd)
            return 1m;

        var activeDayCount = (activeEnd - activeStart).Days + 1;
        return Math.Min(1m, activeDayCount / 30m);
    }

    public static decimal ResolveLineItemMultiplier(
        ChargeTypeBehavior behavior,
        decimal periodMultiplier)
        => behavior == ChargeTypeBehavior.FirstMonthOneTime
            ? 1m
            : periodMultiplier;
}
