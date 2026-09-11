using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Leases;

public static class LeaseSchedulePolicy
{
    public static bool HasValidDateRange(DateTime startDate, DateTime endDate)
        => endDate > startDate;

    public static bool IsValidDueDay(int dueDay)
        => dueDay is >= 1 and <= 31;

    public static bool CanExtend(DateTime currentEndDate, DateTime newEndDate)
        => newEndDate > currentEndDate;

    public static bool IsActive(LeaseStatus status, DateTime startDate, DateTime endDate, DateTime currentTime)
        => status == LeaseStatus.Active
            && startDate <= currentTime
            && endDate >= currentTime;

    public static int GetRemainingDays(DateTime endDate, DateTime currentTime)
        => (int)(endDate - currentTime).TotalDays;

    public static double GetDurationPercentage(DateTime startDate, DateTime endDate, DateTime currentTime)
    {
        var total = (endDate - startDate).TotalDays;
        var elapsed = (currentTime - startDate).TotalDays;
        if (total <= 0) return 100;

        return Math.Min(100, Math.Max(0, elapsed / total * 100));
    }
}
