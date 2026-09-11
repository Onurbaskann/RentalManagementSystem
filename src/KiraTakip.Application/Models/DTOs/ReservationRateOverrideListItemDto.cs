namespace KiraTakip.Models.Dtos;

public class ReservationRateOverrideListItemDto
{
    public int Id { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? PropertyName { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; }
    public decimal PeriodRate { get; set; }
    public decimal KdvRate { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
