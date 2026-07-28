namespace KiraTakip.Models.ViewModels;

public class RateMatrixCell
{
    public int LineItemId { get; set; }
    public int TenantCategoryId { get; set; }
    public int ChargeTypeId { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
}
