namespace KiraTakip.Models.ViewModels;

public class ReservationAreaEditViewModel
{
    public int? Id { get; set; }
    public string? UnitNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal VatRate { get; set; } = 20;
    public bool HasActiveReservation { get; set; }
}
