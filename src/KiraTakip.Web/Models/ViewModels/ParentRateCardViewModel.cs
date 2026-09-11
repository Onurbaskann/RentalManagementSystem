using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public class ParentRateCardViewModel
{
    public string SourceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ParentRateRowViewModel> Rows { get; set; } = [];
}

public class ParentRateRowViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal VatRate { get; set; }
    public string? Source { get; set; }
}
