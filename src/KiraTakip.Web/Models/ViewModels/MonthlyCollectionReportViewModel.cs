namespace KiraTakip.Web.Models.ViewModels;

public class MonthlyCollectionReportViewModel
{
    public int Year { get; set; }
    public List<MonthlyCollectionReportRowViewModel> Rows { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
    public List<ReportOptionViewModel> Stores { get; set; } = [];
    public List<ReportOptionViewModel> ChargeTypes { get; set; } = [];
    public int? SelectedStoreId { get; set; }
    public int? SelectedChargeTypeId { get; set; }
    public string? SelectedStoreName => SelectedStoreId.HasValue
        ? Stores.FirstOrDefault(store => store.Id == SelectedStoreId.Value)?.Name
        : null;

    public decimal TotalExpected => Rows.Sum(row => row.ExpectedAmount);
    public decimal TotalCollected => Rows.Sum(row => row.CollectedAmount);
    public int TotalCollectedPaymentCount => Rows.Sum(row => row.CollectedPaymentCount);
    public int TotalOverdueCount => Rows.Sum(row => row.OverdueChargeCount);
    public decimal TotalOverdueAmount => Rows.Sum(row => row.OverdueAmount);
    public double OverallCollectionRate => TotalExpected > 0
        ? (double)(TotalCollected / TotalExpected * 100)
        : 0;

    // Tahakkuk kartı alt yazısı — kaynak tipi kırılımı.
    public int LeaseChargeCount { get; set; }
    public int ManualChargeCount { get; set; }
    public int ReservationChargeCount { get; set; }

    // Ödenen kartı kırılımı — o yılın tahakkukuna yapılan ödemelerin, ödeme tarihinin yılına göre dağılımı.
    public List<ReportYearCountViewModel> OdenenByPaymentYear { get; set; } = [];

    // Nakit Bazlı blok — o yıl içinde yapılan ödemelerin, ait oldukları tahakkukun yılına göre dağılımı.
    public List<ReportYearAmountViewModel> CashBasisByChargeYear { get; set; } = [];
    public decimal CashBasisTotalCollected => CashBasisByChargeYear.Sum(item => item.Amount);
    public int CashBasisPaymentCount => CashBasisByChargeYear.Sum(item => item.Count);

    // Gecikme bloğu — geçmiş yıllardan taşınan, hâlâ ödenmemiş tahakkuklar.
    public int PriorYearsOverdueCount { get; set; }
    public decimal PriorYearsOverdueAmount { get; set; }
}

public record ReportOptionViewModel(int Id, string Name);
public record ReportYearCountViewModel(int Year, int Count);
public record ReportYearAmountViewModel(int Year, int Count, decimal Amount);
