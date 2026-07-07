namespace KiraTakip.Models.DTOs;

public class ChargeLineItemPreview
{
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public ChargeTypeBehavior Behavior { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal Multiplier { get; set; }
    public decimal Amount { get; set; }
    public decimal KdvRate { get; set; }
    public decimal KdvAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public LineItemSourceType SourceType { get; set; }
    public bool IsRateFound { get; set; }
    public string? Description { get; set; }
}
