namespace KiraTakip.Domain.Reservations;

public static class ReservationAttendeePolicy
{
    public static bool IsWithinMaximumCount(int attendeeCount, int maximumAttendeeCount)
        => attendeeCount <= maximumAttendeeCount;

    public static bool HasExactlyOneOwner(int ownerCount)
        => ownerCount == 1;
}
