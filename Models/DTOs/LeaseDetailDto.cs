namespace KiraTakip.Models.Dtos;

public class LeaseDetailDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }
    public string? TenantEmail { get; set; }
    public int? TenantCategoryId { get; set; }
    public string? TenantCategoryName { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string? UnitNo { get; set; }
    public int? UnitFloorNo { get; set; }
    public decimal UnitArea { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string PropertyDistrict { get; set; } = string.Empty;
    public string PropertyNeighborhood { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public LeaseStatus Status { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public bool IsVatApplied { get; set; }
    public DueDateRuleType DueDateRuleType { get; set; }
    public int DueDay { get; set; }
    public List<LeaseActivityLogDto> ActivityLog { get; set; } = [];
    public List<LeaseRateDto> LeaseRateOverrides { get; set; } = [];
}
