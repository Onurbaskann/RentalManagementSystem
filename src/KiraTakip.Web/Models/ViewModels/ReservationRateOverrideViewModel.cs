using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Web.Models.ViewModels;

public class ReservationRateOverrideViewModel
{
    public int Id { get; set; }
    public int? SpaceId { get; set; }
    public int? ChargeTypeId { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; } = 60;
    public decimal PeriodRate { get; set; }
    public decimal KdvRate { get; set; } = 20;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ReservationSpaceOptionDto> Spaces { get; set; } = [];
    public List<ChargeTypeLookupDto> ChargeTypes { get; set; } = [];
}
