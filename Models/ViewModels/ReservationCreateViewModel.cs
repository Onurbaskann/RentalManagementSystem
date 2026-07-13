using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ReservationCreateViewModel
{
    public int? UnitId { get; set; }
    public int? TenantId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddHours(2);
    public string? Description { get; set; }
    public List<UnitListItemDto> Units { get; set; } = [];
    public List<KiraciListItemDto> Tenants { get; set; } = [];
    public RezervasyonHesapSonucu? CalculationResult { get; set; }
}
