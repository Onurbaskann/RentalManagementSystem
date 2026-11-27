using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos;

public class ChargeDetailDto
{
    public int Id { get; set; }
    public string ChargeNo { get; set; } = string.Empty;
    public int? LeaseId { get; set; }
    public string? LeaseNo { get; set; }
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public int? SpaceId { get; set; }
    public string? SpaceName { get; set; }
    public string? SpacePath { get; set; }
    // Kapsam denetimi için (tek alan → en çok bir yol).
    public IEnumerable<string> SpacePaths => string.IsNullOrEmpty(SpacePath) ? [] : [SpacePath];
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public ChargeStatus Status { get; set; }
    public ChargeSourceType SourceType { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ChargeLineItemDto> LineItems { get; set; } = [];
    public List<PaymentAllocationDto> Allocations { get; set; } = [];
}
