namespace KiraTakip.Models.Dtos;

public class UnitReservationRateOverrideDto
{
    public int Id { get; set; }
    public int? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal PeriodRate { get; set; }
    public int BillingPeriodMinutes { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal VatRate { get; set; }
}
