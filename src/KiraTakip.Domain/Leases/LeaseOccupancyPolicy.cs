using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Leases;

public static class LeaseOccupancyPolicy
{
    public static OccupancyStatus DetermineStatus(
        bool hasActiveLease,
        int remainingDays,
        int expiringSoonThresholdDays)
    {
        if (!hasActiveLease) return OccupancyStatus.Vacant;

        return remainingDays <= expiringSoonThresholdDays
            ? OccupancyStatus.ExpiringSoon
            : OccupancyStatus.Leased;
    }
}
