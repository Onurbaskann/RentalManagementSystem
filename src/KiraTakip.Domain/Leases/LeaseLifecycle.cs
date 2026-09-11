using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Leases;

public static class LeaseLifecycle
{
    public static bool CanExtend(LeaseStatus status)
        => status == LeaseStatus.Active;

    public static bool CanTerminate(LeaseStatus status)
        => status == LeaseStatus.Active;

    public static bool CanUpdateDueDate(LeaseStatus status)
        => status == LeaseStatus.Active;

    public static bool CanRegenerateCharges(LeaseStatus status)
        => status == LeaseStatus.Active;

    public static bool CanGenerateCharges(LeaseStatus status)
        => status == LeaseStatus.Active;
}
