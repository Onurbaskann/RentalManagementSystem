namespace KiraTakip.Models.Dtos;

public class UnitCustomRateDto
{
    public int Id { get; set; }
    public string TenantCategoryName { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
}
