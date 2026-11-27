using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos;

public class LeaseAreaDto
{
    public int SpaceId { get; set; }
    public string SpaceName { get; set; } = string.Empty;
    public string SpaceIdPath { get; set; } = string.Empty;
    public decimal SpaceArea { get; set; }
    public decimal RentedArea { get; set; }
    public string SnapshotCode { get; set; } = string.Empty;
    public string SnapshotName { get; set; } = string.Empty;
    public string SnapshotTypeName { get; set; } = string.Empty;
    public SpaceUsage SnapshotUsage { get; set; }
    public decimal SnapshotArea { get; set; }
    public string SnapshotPath { get; set; } = string.Empty;
}

public class LeaseListItemDto
{
    public int Id { get; set; }
    public string LeaseNo { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public string TenantCategoryName { get; set; } = string.Empty;
    public LeaseAreaDto? Area { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public bool IsRentFree { get; set; }
    public LeaseStatus Status { get; set; }

    public int SpaceId => Area?.SpaceId ?? 0;
    public string SpaceName => Area?.SpaceName ?? string.Empty;
    public string SpacePath => Area?.SnapshotPath ?? string.Empty;
    public string SpaceIdPath => Area?.SpaceIdPath ?? string.Empty;
    public decimal SpaceArea => Area?.SpaceArea ?? 0m;
    public decimal RentedArea => Area?.RentedArea ?? 0m;

    public int RemainingDays => (int)(EndDate - DateTime.Now).TotalDays;
    public bool IsActive => Status == LeaseStatus.Active && StartDate <= DateTime.Now && EndDate >= DateTime.Now;
}
