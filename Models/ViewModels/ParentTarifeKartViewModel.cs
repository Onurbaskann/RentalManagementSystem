namespace KiraTakip.Models.ViewModels;

public enum TarifeHiyerarsiKatmani
{
    Property = 1,
    Unit    = 2,
    Lease = 3
}

public class ParentTarifeKartViewModel
{
    public string SourceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ParentTarifeSatir> Rows { get; set; } = [];
}

public class ParentTarifeSatir
{
    public string CategoryName { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
    public string? Source { get; set; }
}
