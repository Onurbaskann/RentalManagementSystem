namespace KiraTakip.Models.ViewModels;

public class TarifeKalemSatiri
{
    public int KalemId { get; set; }
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
}
