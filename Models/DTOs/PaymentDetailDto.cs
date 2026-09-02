namespace KiraTakip.Models.Dtos;

public class PaymentDetailDto
{
    public int Id { get; set; }
    public int ChargeId { get; set; }
    public int ChargeLineItemId { get; set; }
    public string ChargeLineItemDescription { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public int StoreAccountId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreProviderCode { get; set; } = string.Empty;
    public string StoreCurrency { get; set; } = string.Empty;
    public int? LeaseId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentSourceType PaymentSourceType { get; set; }
    public string? PosReferenceNo { get; set; }
    public string? Description { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
    public int? PropertyId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public DateTime ChargePeriodStart { get; set; }
    public string? CreatedByUserDisplayName { get; set; }
    public string? ApprovedByUserDisplayName { get; set; }
    public List<PaymentBankMatchDto> BankMatches { get; set; } = [];
}
