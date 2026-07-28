namespace KiraTakip.Models.ViewModels;

public class MonthlyCollectionReportViewModel
{
    public int Year { get; set; }
    public List<MonthlyCollectionReportRowViewModel> Rows { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
    public decimal TotalExpected => Rows.Sum(row => row.ExpectedAmount);
    public decimal TotalCollected => Rows.Sum(row => row.CollectedAmount);
    public int TotalOverdueCount => Rows.Sum(row => row.OverdueChargeCount);
    public decimal TotalOverdueAmount => Rows.Sum(row => row.OverdueAmount);
    public double OverallCollectionRate => TotalExpected > 0
        ? (double)(TotalCollected / TotalExpected * 100)
        : 0;
}
