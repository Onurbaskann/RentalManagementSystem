using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Reservations;

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

    public static bool CanCancel(ReservationStatus status)
        => CanTransition(status, ReservationStatus.Cancelled);

    public static bool CanModify(ReservationStatus status)
        => status is ReservationStatus.PendingApproval
            or ReservationStatus.Confirmed;

    public static bool BlocksAvailability(ReservationStatus status)
        => status == ReservationStatus.Confirmed;

    public static bool CanComplete(ReservationStatus status)
        => CanTransition(status, ReservationStatus.Completed);

    public static bool CanTransferToCharge(ReservationStatus status)
        => status == ReservationStatus.Confirmed;
}
