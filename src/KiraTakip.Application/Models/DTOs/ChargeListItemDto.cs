using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos;

public class ChargeListItemDto
{
    public int Id { get; set; }
    public string ChargeNo { get; set; } = string.Empty;
    public int? LeaseId { get; set; }
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public int? SpaceId { get; set; }
    public string? SpaceName { get; set; }
    public string? SpacePath { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public ChargeStatus Status { get; set; }
    public ChargeSourceType SourceType { get; set; }
    public int PendingPaymentCount { get; set; }
    public decimal PendingPaymentAmount { get; set; }
    public List<ChargeLineItemDto> LineItems { get; set; } = [];
}
