using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ReservationRateOverrideViewModel
{
    public int Id { get; set; }
    public int? UnitId { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; } = 60;
    public decimal PeriodRate { get; set; }
    public decimal KdvRate { get; set; } = 20;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<UnitListItemDto> ReservableUnits { get; set; } = [];
}
