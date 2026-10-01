namespace KiraTakip.Models.Dtos.Report;

public record GetMonthlyCollectionReportInput(
    int Year,
    DateTime Today,
    int? StoreId = null,
    int? ChargeTypeId = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public class MonthlyCollectionReportRowDto
{
    public int Month { get; set; }
    public int ChargeCount { get; set; }
    public decimal ExpectedAmount { get; set; }
    public int OverdueChargeCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public int CollectedPaymentCount { get; set; }
}

public class MonthlyCollectionReportDto
{
    public int Year { get; set; }
    public List<MonthlyCollectionReportRowDto> Rows { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
    public List<ReportStoreOptionDto> Stores { get; set; } = [];
    public List<ReportChargeTypeOptionDto> ChargeTypes { get; set; } = [];
    public int? SelectedStoreId { get; set; }
    public int? SelectedChargeTypeId { get; set; }

    // Tahakkuk kartı alt yazısı — kaynak tipi kırılımı (yıl boyunca, distinct tahakkuk).
    public int LeaseChargeCount { get; set; }
    public int ManualChargeCount { get; set; }
    public int ReservationChargeCount { get; set; }

    // Ödenen kartı kırılımı — o yılın tahakkukuna yapılan ödemelerin, ödeme tarihinin yılına göre dağılımı.
    public List<ReportYearCountDto> OdenenByPaymentYear { get; set; } = [];

    // Nakit Bazlı blok — o yıl içinde yapılan ödemelerin, ait oldukları tahakkukun yılına göre dağılımı.
    public List<ReportYearAmountDto> CashBasisByChargeYear { get; set; } = [];

    // Gecikme bloğu — geçmiş yıllardan taşınan, hâlâ ödenmemiş tahakkuklar.
    public int PriorYearsOverdueCount { get; set; }
    public decimal PriorYearsOverdueAmount { get; set; }
}

public record ReportStoreOptionDto(int Id, string Name);
public record ReportChargeTypeOptionDto(int Id, string Name);

public record PriorYearsOverdueSummary(int OverdueChargeCount, decimal OverdueAmount);

public record ReportYearCountDto(int Year, int Count);
public record ReportYearAmountDto(int Year, int Count, decimal Amount);
