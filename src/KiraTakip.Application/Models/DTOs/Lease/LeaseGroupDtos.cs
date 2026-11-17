using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Lease;

/// <summary>
/// Gruptaki bir sözleşme satırı. UpdateGroup'ta LeaseId dolu satır mevcut sözleşmeyi günceller,
/// null satır yeni sözleşme ekler; listede olmayan mevcut sözleşme gruptan çıkarılır (silinir).
/// </summary>
public sealed record LeaseGroupMemberInput(
    int? LeaseId,
    int TenantId,
    decimal Area,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides);

public sealed record CreateLeaseGroupInput(
    int UnitId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    bool IsRentFree,
    IReadOnlyCollection<LeaseGroupMemberInput> Members,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record UpdateLeaseGroupInput(
    int GroupId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    bool IsRentFree,
    IReadOnlyCollection<LeaseGroupMemberInput> Members,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record ResubmitLeaseGroupInput(
    int GroupId,
    string? Explanation,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record DeleteLeaseGroupDraftInput(
    int GroupId,
    string Explanation,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record ApproveLeaseGroupInput(
    int GroupId,
    string? Explanation,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

/// <summary>Uzatmada sözleşme başına tarife satırları (A2). Listelenmeyen sözleşme için boş kabul edilir.</summary>
public sealed record ExtendLeaseGroupMemberInput(
    int LeaseId,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides);

public sealed record ExtendLeaseGroupInput(
    int GroupId,
    DateTime NewEndDate,
    bool IsVatApplied,
    decimal VatRate,
    decimal? InflationRate,
    string? Description,
    bool UpdateRate,
    bool CanOverrideRate,
    IReadOnlyCollection<ExtendLeaseGroupMemberInput> Members,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record TerminateLeaseGroupInput(
    int GroupId,
    DateTime TerminationDate,
    string TerminationReason,
    string? Description,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

/// <summary>Eski grup feshedilir, yeni grup (PreviousGroupId = GroupId) Draft açılır. Members satırlarında LeaseId null olmalıdır.</summary>
public sealed record RestructureLeaseGroupInput(
    int GroupId,
    DateTime TerminationDate,
    string TerminationReason,
    string? TerminationDescription,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    bool IsRentFree,
    IReadOnlyCollection<LeaseGroupMemberInput> Members,
    string ActorUserId,
    LeaseAccessScopeInput AccessScope);

public sealed record GetLeaseGroupInput(
    int GroupId,
    LeaseAccessScopeInput AccessScope);

public sealed class LeaseGroupMemberDto
{
    public int LeaseId { get; init; }
    public string LeaseNo { get; init; } = string.Empty;
    public int TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public LeaseStatus Status { get; init; }
    public decimal Area { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public IReadOnlyList<LeaseRateDto> RateOverrides { get; init; } = [];
    public LeaseReviewHistoryDto? LatestRevision { get; set; }
}

/// <summary>Grup detayı; aynı zamanda grup düzenleme formunu doldurmak için kullanılır.</summary>
public sealed class LeaseGroupDetailDto
{
    public int GroupId { get; init; }
    public int? PreviousGroupId { get; init; }
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public int PropertyId { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public decimal UnitArea { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DueDateRuleType DueDateRuleType { get; init; }
    public int DueDay { get; init; }
    public string? Description { get; init; }
    public bool IsRentFree { get; init; }
    public string OwnerUserId { get; init; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<LeaseGroupMemberDto> Members { get; init; } = [];
}

/// <summary>Sözleşmenin grup bilgisi (Lease/Draft ekranında gruba bağlantı için).</summary>
public sealed record LeaseGroupLinkDto(int GroupId, int MemberCount);
