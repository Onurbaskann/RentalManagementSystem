using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Web.Models.ViewModels;

public class ReservationCalendarQueryViewModel
{
    public DateTime? Date { get; set; }
    public int? SpaceId { get; set; }

    public GetReservationCalendarInput ToInternalInput(SpaceAccessScope accessScope)
        => new(Date, SpaceId, accessScope);

    public GetTenantReservationCalendarInput ToTenantInput(
        int tenantId,
        SpaceAccessScope accessScope)
        => new(tenantId, Date, SpaceId, accessScope);
}

public class ReservationAvailabilityQueryViewModel
{
    public int? SpaceId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ExcludedReservationId { get; set; }

    public bool TryMap(
        SpaceAccessScope accessScope,
        out CheckReservationAvailabilityInput? input)
    {
        if (!SpaceId.HasValue || !StartDate.HasValue || !EndDate.HasValue)
        {
            input = null;
            return false;
        }

        input = new CheckReservationAvailabilityInput(
            SpaceId.Value,
            StartDate.Value,
            EndDate.Value,
            ExcludedReservationId,
            accessScope);
        return true;
    }
}
