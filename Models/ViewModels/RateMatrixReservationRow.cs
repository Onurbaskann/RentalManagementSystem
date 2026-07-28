namespace KiraTakip.Models.ViewModels;

public class RateMatrixReservationRow
{
    public int ReservationRateId { get; set; }
    public int UnitTypeId { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; }
    public decimal PeriodRate { get; set; }
    public decimal KdvRate { get; set; }
}
