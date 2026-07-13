namespace KiraTakip.Models.ViewModels;

public class ParentReservationRateOverrideCardViewModel
{
    public string SourceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ParentReservationRateOverrideRow> Rows { get; set; } = [];
}

public class ParentReservationRateOverrideRow
{
    public string UnitTypeName { get; set; } = string.Empty;
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; }
    public decimal PeriodRate { get; set; }
    public decimal KdvRate { get; set; }
}
