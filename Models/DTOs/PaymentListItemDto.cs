namespace KiraTakip.Models.Dtos;

public class PaymentListItemDto
{
    public int Id { get; set; }
    public int ChargeId { get; set; }
    public int ChargeLineItemId { get; set; }
    public string ChargeLineItemDescription { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public int? LeaseId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentSourceType PaymentSourceType { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public DateTime ChargePeriodStart { get; set; }
    public string? CreatedByUserDisplayName { get; set; }
}
