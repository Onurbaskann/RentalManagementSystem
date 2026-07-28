namespace KiraTakip.Models.Dtos;

public class LeaseLineItemInputDto
{
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public ChargeTypeBehavior Behavior { get; set; }
    public decimal DefaultAmount { get; set; }
    public decimal Amount { get; set; }
    public decimal UnitValue { get; set; }
    public decimal DefaultUnitValue { get; set; }
    public decimal VatRate { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public bool IsUserModified { get; set; }
    public bool IsRateFound { get; set; }
    public string? SourceType { get; set; }
}
