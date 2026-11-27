namespace KiraTakip.Models.Dtos;

public class ReservationRateOverrideListItemDto
{
    public int Id { get; set; }
    public int SpaceId { get; set; }
    public string SpaceName { get; set; } = string.Empty;
    public string SpacePath { get; set; } = string.Empty;
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; }
    public decimal PeriodRate { get; set; }
    public decimal KdvRate { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
