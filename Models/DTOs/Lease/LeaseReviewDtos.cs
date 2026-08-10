namespace KiraTakip.Models.Dtos;

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
    public int UnitId { get; init; }
    public int TenantId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DueDateRuleType DueDateRuleType { get; init; }
    public int DueDay { get; init; }
    public string? Description { get; init; }
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
    LeaseAccessScopeInput AccessScope);

public sealed record GetLeaseDraftInput(
    int LeaseId,
    LeaseAccessScopeInput AccessScope);

public sealed record CreateLeaseDraftInput(
    int UnitId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record UpdateLeaseDraftInput(
    int LeaseId,
    int UnitId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record ResubmitLeaseRevisionInput(
    int LeaseId,
    int UnitId,
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
    LeaseAccessScopeInput AccessScope);

public sealed record RequestLeaseRevisionInput(
    int LeaseId,
    string Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record ApproveLeaseInput(
    int LeaseId,
    string? Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record DeleteLeaseDraftInput(
    int LeaseId,
    string Explanation,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);
