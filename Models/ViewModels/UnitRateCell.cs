namespace KiraTakip.Models.ViewModels;

public class UnitRateCell
{
    public int TenantCategoryId { get; set; }
    public int ChargeTypeId { get; set; }
    public bool IsCustomRateActive { get; set; }
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }

    // Fallback/Varsayılan değer bilgileri
    public decimal DefaultUnitValue { get; set; }
    public decimal DefaultKdvRate { get; set; }
    public CalculationMethod DefaultCalculationMethod { get; set; } = CalculationMethod.Fixed;
    public string DefaultSource { get; set; } = string.Empty;
}
