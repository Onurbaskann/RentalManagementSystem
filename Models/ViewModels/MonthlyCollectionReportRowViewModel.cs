namespace KiraTakip.Models.ViewModels;

public class MonthlyCollectionReportRowViewModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int ChargeCount { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public int OverdueChargeCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public double CollectionRate => ExpectedAmount > 0
        ? (double)(CollectedAmount / ExpectedAmount * 100)
        : 0;
}
