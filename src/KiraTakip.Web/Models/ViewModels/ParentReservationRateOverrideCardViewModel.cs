namespace KiraTakip.Web.Models.ViewModels;

public class ParentReservationRateOverrideCardViewModel
{
    public string SourceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ParentReservationRateOverrideRow> Rows { get; set; } = [];
}

public class ParentReservationRateOverrideRow
{
    public string SpaceName { get; set; } = string.Empty;
    public int FreeDurationMinutes { get; set; }
    public int BillingPeriodMinutes { get; set; }
    public decimal PeriodRate { get; set; }
    public decimal VatRate { get; set; }
}
