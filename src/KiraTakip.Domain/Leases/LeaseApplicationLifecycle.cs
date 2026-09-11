using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Leases;

public static class LeaseApplicationLifecycle
{
    public static bool CanTransition(LeaseStatus from, LeaseStatus to)
        => (from, to) switch
        {
            (LeaseStatus.Draft, LeaseStatus.RevisionRequested) => true,
            (LeaseStatus.RevisionRequested, LeaseStatus.Draft) => true,
            (LeaseStatus.Draft, LeaseStatus.Active) => true,
            _ => false
        };

    public static bool CanEditDraft(LeaseStatus status)
        => status == LeaseStatus.Draft;

    public static bool CanDeleteApplication(LeaseStatus status)
        => status is LeaseStatus.Draft or LeaseStatus.RevisionRequested;
}
