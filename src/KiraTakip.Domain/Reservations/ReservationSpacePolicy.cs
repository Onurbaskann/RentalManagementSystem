using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Reservations;

public static class ReservationSpacePolicy
{
    public static bool IsReservable(bool isSpaceActive, SpaceUsage usage)
        => isSpaceActive && usage == SpaceUsage.Reservable;
}
