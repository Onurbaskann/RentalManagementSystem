using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos;

public class LeaseDetailDto
{
    public int Id { get; set; }
    public string LeaseNo { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }
    public string? TenantEmail { get; set; }
    public int? TenantCategoryId { get; set; }
    public string? TenantCategoryName { get; set; }
    public LeaseAreaDto? Area { get; set; }
    public int? LeaseGroupId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public LeaseStatus Status { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public bool IsVatApplied { get; set; }
    public bool IsRentFree { get; set; }
    public DueDateRuleType DueDateRuleType { get; set; }
    public int DueDay { get; set; }
    public List<LeaseActivityLogDto> ActivityLog { get; set; } = [];
    public List<LeaseRateDto> LeaseRateOverrides { get; set; } = [];

    public int SpaceId => Area?.SpaceId ?? 0;
    public string SpaceName => Area?.SpaceName ?? string.Empty;
    public string SpacePath => Area?.SnapshotPath ?? string.Empty;
    public string SpaceIdPath => Area?.SpaceIdPath ?? string.Empty;
    public decimal SpaceArea => Area?.SpaceArea ?? 0m;
    public decimal? RentedArea => Area?.RentedArea;
}
