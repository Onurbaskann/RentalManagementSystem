namespace KiraTakip.Models.Dtos;

public class LeaseRateDto
{
    public int Id { get; set; }
    public int ChargeTypeId { get; set; }
    public string ChargeTypeCode { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public ChargeTypeBehavior ChargeTypeBehavior { get; set; }
    public decimal UnitValue { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal KdvRate { get; set; }
}
