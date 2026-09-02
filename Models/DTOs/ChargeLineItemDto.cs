namespace KiraTakip.Models.Dtos;

public class ChargeLineItemDto
{
    public int Id { get; set; }
    public int ChargeTypeId { get; set; }
    public string ChargeTypeCode { get; set; } = string.Empty;
    public int ChargeTypeSortOrder { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal Multiplier { get; set; }
    public decimal Amount { get; set; }
    public decimal KdvRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public LineItemSourceType SourceType { get; set; }

    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public decimal AvailableAmount => TotalAmount - PaidAmount - PendingAmount;
}
