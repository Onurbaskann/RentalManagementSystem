namespace KiraTakip.Models.Dtos;

public record GetMonthlyCollectionReportInput(
    int Year,
    DateTime Today,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public class MonthlyCollectionReportRowDto
{
    public int Month { get; set; }
    public int ChargeCount { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public int OverdueChargeCount { get; set; }
    public decimal OverdueAmount { get; set; }
}

public class MonthlyCollectionReportDto
{
    public int Year { get; set; }
    public List<MonthlyCollectionReportRowDto> Rows { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
}
