namespace KiraTakip.Models;

public static class ReservationLifecycle
{
    public static bool CanTransition(ReservationStatus from, ReservationStatus to)
        => (from, to) switch
        {
            (ReservationStatus.PendingApproval, ReservationStatus.Confirmed) => true,
            (ReservationStatus.PendingApproval, ReservationStatus.Rejected) => true,
            (ReservationStatus.PendingApproval, ReservationStatus.Cancelled) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Completed) => true,
            _ => false
        };

    public static bool IsTerminal(ReservationStatus status)
        => status is ReservationStatus.Rejected
            or ReservationStatus.Cancelled
            or ReservationStatus.Completed;

    public static bool BlocksAvailability(ReservationStatus status)
        => status == ReservationStatus.Confirmed;
}
