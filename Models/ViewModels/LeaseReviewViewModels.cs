using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public interface ILeaseFormViewModel
{
    int? UnitId { get; set; }
    int TenantId { get; set; }
    DateTime StartDate { get; set; }
    DateTime EndDate { get; set; }
    DueDateRuleType DueDateRuleType { get; set; }
    int DueDay { get; set; }
    string? Description { get; set; }
    List<UnitLookupDto> AvailableUnits { get; set; }
    List<TenantListItemDto> Tenants { get; set; }
    List<LeaseLineItemInputDto> LeaseLineItems { get; set; }
}

public sealed class LeaseDraftViewModel : ILeaseFormViewModel
{
    public int LeaseId { get; set; }
    public int? UnitId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DueDateRuleType DueDateRuleType { get; set; }
    public int DueDay { get; set; }
    public string? Description { get; set; }
    public LeaseStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanRequestRevision { get; set; }
    public bool CanDelete { get; set; }
    public List<UnitLookupDto> AvailableUnits { get; set; } = [];
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
