using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ReservationCalendarQueryViewModel
{
    public DateTime? Date { get; set; }
    public int? UnitId { get; set; }

    public GetReservationCalendarInput ToInternalInput(ReservationAccessScopeInput accessScope)
        => new(Date, UnitId, accessScope);

    public GetTenantReservationCalendarInput ToTenantInput(
        int tenantId,
        ReservationAccessScopeInput accessScope)
        => new(tenantId, Date, UnitId, accessScope);
}

public class ReservationAvailabilityQueryViewModel
{
    public int? UnitId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ExcludedReservationId { get; set; }

    public bool TryMap(
        ReservationAccessScopeInput accessScope,
        out CheckReservationAvailabilityInput? input)
    {
        if (!UnitId.HasValue || !StartDate.HasValue || !EndDate.HasValue)
        {
            input = null;
            return false;
        }

        input = new CheckReservationAvailabilityInput(
            UnitId.Value,
            StartDate.Value,
            EndDate.Value,
            ExcludedReservationId,
            accessScope);
        return true;
    }
}
