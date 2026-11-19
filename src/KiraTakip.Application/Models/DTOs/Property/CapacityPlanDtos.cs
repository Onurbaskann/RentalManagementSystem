using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Property;

public record CapacityPlanInput(
    int UnitId,
    int NewCapacity,
    LeaseAccessScopeInput AccessScope,
    IReadOnlyDictionary<int, decimal>? Overrides = null,
    bool Optimize = false);

public record UpdateAllocatedAreaInput(
    int LeaseId,
    decimal AllocatedArea,
    LeaseAccessScopeInput AccessScope);

public record LeaseAreaHistoryItemDto(
    DateTime EffectiveFrom,
    decimal AllocatedArea,
    LeaseAreaMode Mode,
    LeaseAreaChangeReason Reason,
    DateTime CreatedAt,
    string CreatedBy);

public record LeaseAllocatedAreaInfoDto(
    decimal RentedArea,
    decimal AllocatedArea,
    LeaseAreaMode Mode,
    decimal MinimumUsageArea,
    bool IsEditable,
    IReadOnlyList<LeaseAreaHistoryItemDto> History);

public record CapacityPlanLeaseDto(
    int LeaseId,
    string LeaseNo,
    string TenantName,
    DateTime StartDate,
    DateTime EndDate,
    LeaseStatus Status,
    decimal? RentedArea,
    decimal AllocatedArea,
    LeaseAreaMode? Mode);

public record CapacityPlanRowDto(
    int LeaseId,
    string LeaseNo,
    string TenantName,
    DateTime StartDate,
    DateTime EndDate,
    LeaseStatus Status,
    decimal? RentedArea,
    LeaseAreaMode? CurrentMode,
    decimal CurrentAllocatedArea,
    decimal ProposedAllocatedArea,
    LeaseAreaMode? ProposedMode,
    bool Editable,
    string? Error);

public record CapacityPlanErrorDto(string Code, string Message, DateTime? Day = null);

public record CapacityPlanWarningDto(
    string Code,
    string Message,
    int FreeSlots,
    decimal RemainingArea,
    decimal NormTarget,
    decimal MinimumTarget,
    DateTime? Day = null);

public record CapacityOptimizationSuggestionDto(int LeaseId, decimal AllocatedArea);

public record CapacityOptimizationShortfallDto(DateTime Day, decimal MissingArea);

public record CapacityOptimizationDto(
    bool IsNeeded,
    IReadOnlyList<CapacityOptimizationSuggestionDto> Suggestions,
    IReadOnlyList<CapacityOptimizationShortfallDto> Shortfalls);

public record CapacityPlanPreviewDto(
    int UnitId,
    int CurrentCapacity,
    int NewCapacity,
    decimal UnitArea,
    decimal MinimumUsageArea,
    decimal EffectiveNorm,
    int MaximumCapacity,
    decimal TotalAllocatedToday,
    decimal PeakAllocated,
    DateTime PeakDay,
    IReadOnlyList<CapacityPlanRowDto> Rows,
    IReadOnlyList<CapacityPlanErrorDto> Errors,
    IReadOnlyList<CapacityPlanWarningDto> Warnings,
    string Hash,
    CapacityOptimizationDto? Optimization = null)
{
    public bool HasErrors => Errors.Count > 0;
}

public static class CapacityPlanCodes
{
    public const string Invalid = "Capacity.Invalid";
    public const string AboveMaximum = "Capacity.AboveMaximum";
    public const string CountExceeded = "Capacity.CountExceeded";
    public const string AreaOverflow = "Capacity.AreaOverflow";
    public const string AreaOutOfRange = "Capacity.AreaOutOfRange";
    public const string PreviewOutdated = "Capacity.PreviewOutdated";
    public const string NormTargetShort = "Capacity.NormTargetShort";
    public const string MinimumTargetShort = "Capacity.MinimumTargetShort";
    public const string UnitNotFound = "Capacity.UnitNotFound";
    public const string UnitNotCapacityBased = "Capacity.UnitNotCapacityBased";
    public const string OutOfScope = "Capacity.OutOfScope";
    public const string CapacityField = "NewCapacity";
    public const string LeaseNotFound = "Capacity.LeaseNotFound";
    public const string AllocatedAreaField = "AllocatedArea";
    public const string AllocatedAreaOutOfRange = "Lease.AllocatedAreaOutOfRange";
    public const string AllocatedAreaNotEditable = "Lease.AllocatedAreaNotEditable";
    public const string AllocatedAreaMissing = "Lease.AllocatedAreaMissing";
}
