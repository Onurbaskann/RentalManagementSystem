using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Reservations;

public static class ReservationUnitPolicy
{
    public static bool IsReservable(
        bool isUnitActive,
        bool isUnitTypeActive,
        UnitTypeUsage usage)
        => isUnitActive
            && isUnitTypeActive
            && usage == UnitTypeUsage.Reservable;
}
