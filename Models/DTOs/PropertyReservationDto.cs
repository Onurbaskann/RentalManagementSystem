namespace KiraTakip.Models.Dtos;

public class PropertyReservationDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal TotalAmount { get; set; }
    public ReservationStatus Status { get; set; }
}
