using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Web.Models.ViewModels;

public interface ILeaseFormViewModel
{
    int? SpaceId { get; set; }
    int TenantId { get; set; }
    DateTime StartDate { get; set; }
    DateTime EndDate { get; set; }
    DueDateRuleType DueDateRuleType { get; set; }
    int DueDay { get; set; }
    string? Description { get; set; }
    bool IsRentFree { get; set; }
    decimal? RentedArea { get; set; }
    List<LeaseSpaceOptionDto> AvailableSpaces { get; set; }
    List<TenantListItemDto> Tenants { get; set; }
    List<LeaseLineItemInputDto> LeaseLineItems { get; set; }
}

public sealed class LeaseDraftViewModel : ILeaseFormViewModel
{
    public int LeaseId { get; set; }
    public string LeaseNo { get; set; } = string.Empty;
    public int? SpaceId { get; set; }
    public int TenantId { get; set; }
    public string SpacePath { get; set; } = string.Empty;
    public string SpaceTypeNameSnapshot { get; set; } = string.Empty;
    public SpaceUsage SpaceUsageSnapshot { get; set; }
    public decimal SpaceAreaSnapshot { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DueDateRuleType DueDateRuleType { get; set; }
    public int DueDay { get; set; }
    public string? Description { get; set; }
    public bool IsRentFree { get; set; }
    public decimal? RentedArea { get; set; }
    public LeaseStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanRequestRevision { get; set; }
    public bool CanDelete { get; set; }
    public int? GroupId { get; set; }
    public int GroupMemberCount { get; set; }
    public bool IsGrouped => GroupId.HasValue;
    public List<LeaseSpaceOptionDto> AvailableSpaces { get; set; } = [];
    public List<TenantListItemDto> Tenants { get; set; } = [];
    public List<LeaseLineItemInputDto> LeaseLineItems { get; set; } = [];
    public IReadOnlyList<LeaseReviewHistoryDto> ReviewHistory { get; set; } = [];
    public LeaseReviewHistoryDto? LatestRevision { get; set; }
    public List<LeaseDraftDocumentViewModel> Documents { get; set; } = [];
    public List<LeaseDraftDocumentTypeViewModel> DocumentTypes { get; set; } = [];
}

public sealed record LeaseDraftDocumentViewModel(
    int Id,
    int DocumentTypeId,
    string DocumentTypeName,
    string FileName,
    long FileSize,
    string? Description);

public sealed record LeaseDraftDocumentTypeViewModel(
    int Id,
    string Name,
    string? Description,
    bool Required,
    string AllowedExtensions,
    int MaxSizeMb);

public sealed class RequestLeaseRevisionViewModel
{
    public int LeaseId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DeleteLeaseDraftViewModel
{
    public int LeaseId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ApproveLeaseViewModel
{
    public int LeaseId { get; set; }
    public string? Explanation { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class UploadLeaseDraftDocumentViewModel
{
    public int LeaseId { get; set; }
    public int DocumentTypeId { get; set; }
    public IFormFile? File { get; set; }
}
