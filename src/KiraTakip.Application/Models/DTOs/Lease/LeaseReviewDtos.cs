using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Lease;

public sealed record LeaseReviewHistoryDto(
    int Id,
    LeaseReviewActionType ActionType,
    LeaseStatus? FromStatus,
    LeaseStatus? ToStatus,
    string? Explanation,
    string ActorDisplayName,
    DateTime ActionDate);

public sealed class LeaseDraftEditDto
{
    public int LeaseId { get; init; }
    public string LeaseNo { get; init; } = string.Empty;
    public int SpaceId { get; init; }
    public int TenantId { get; init; }
    public string SpacePath { get; init; } = string.Empty;
    public string SpaceTypeNameSnapshot { get; init; } = string.Empty;
    public SpaceUsage SpaceUsageSnapshot { get; init; }
    public decimal SpaceAreaSnapshot { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DueDateRuleType DueDateRuleType { get; init; }
    public int DueDay { get; init; }
    public string? Description { get; init; }
    public bool IsRentFree { get; init; }
    public decimal? RentedArea { get; init; }
    public LeaseStatus Status { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public string OwnerUserId { get; init; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<LeaseRateDto> RateOverrides { get; init; } = [];
    public LeaseReviewHistoryDto? LatestRevision { get; set; }
}

public sealed record GetTenantPortalLeasesInput(
    int TenantId,
    SpaceAccessScope AccessScope);

public sealed record GetTenantPortalLeasesPageInput(
    int TenantId,
    KiraTakip.Models.Common.TableQuery Query,
    SpaceAccessScope AccessScope);

public sealed record GetLeaseDraftInput(
    int LeaseId,
    SpaceAccessScope AccessScope);

public sealed record CreateLeaseDraftInput(
    int SpaceId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    string ActorUserId,
    SpaceAccessScope AccessScope,
    bool IsRentFree = false,
    decimal? RentedArea = null);

public sealed record UpdateLeaseDraftInput(
    int LeaseId,
    int SpaceId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope,
    bool IsRentFree = false,
    decimal? RentedArea = null);

public sealed record ResubmitLeaseRevisionInput(
    int LeaseId,
    int SpaceId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    string? Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope,
    bool IsRentFree = false,
    decimal? RentedArea = null);

public sealed record RequestLeaseRevisionInput(
    int LeaseId,
    string Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope);

public sealed record ApproveLeaseInput(
    int LeaseId,
    string? Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope);

public sealed record DeleteLeaseDraftInput(
    int LeaseId,
    string Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope);
