namespace KiraTakip.Models.Dtos;

public class ManualChargeListItemDto
{
    public int Id { get; set; }
    public int? LeaseId { get; set; }
    public string? TenantDisplayName { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitName { get; set; }
    public string? ChargeTypeCode { get; set; }
    public string? FirstLineItemDescription { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public ChargeStatus Status { get; set; }
    public string? CancellationNote { get; set; }
    public int TenantId { get; set; }
    public string? TenantCategoryName { get; set; }
}
