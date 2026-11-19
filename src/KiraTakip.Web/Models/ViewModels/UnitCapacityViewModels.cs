using HashidsNet;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public class UnitCapacityPreviewRequest
{
    public int NewCapacity { get; set; }
    public Dictionary<string, decimal>? Overrides { get; set; }
    public bool Optimize { get; set; }
}

public class UnitCapacityApplyViewModel
{
    public int NewCapacity { get; set; }
    public string? ExpectedHash { get; set; }
    public Dictionary<string, decimal>? Overrides { get; set; }
}

public class UnitCapacityPageViewModel
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public bool CanApply { get; set; }
    public UnitCapacityPreviewJson Preview { get; set; } = null!;
    public Dictionary<string, decimal> Overrides { get; set; } = [];
}

public record UnitCapacityRowJson(
    string Id,
    string LeaseNo,
    string Tenant,
    string Start,
    string End,
    string Status,
    decimal? RentedArea,
    string? CurrentMode,
    decimal CurrentArea,
    decimal ProposedArea,
    string? ProposedMode,
    bool Manual,
    bool Editable,
    string? Error);

public record UnitCapacityIssueJson(string Code, string Message, IReadOnlyList<string> RowIds);

public record UnitCapacityPreviewJson(
    int CurrentCapacity,
    int NewCapacity,
    decimal UnitArea,
    decimal MinimumUsageArea,
    decimal Norm,
    int MaximumCapacity,
    decimal TotalToday,
    decimal Peak,
    string PeakDay,
    IReadOnlyList<UnitCapacityRowJson> Rows,
    IReadOnlyList<UnitCapacityIssueJson> Errors,
    IReadOnlyList<UnitCapacityIssueJson> Warnings,
    string Hash,
    bool HasErrors,
    bool CountExceeded,
    int FreeSlots,
    decimal? NormShortArea,
    bool MinimumShort,
    decimal? MinimumShortArea,
    UnitCapacityOptimizationJson? Optimization);

public record UnitCapacitySuggestionJson(string Id, decimal Area);

public record UnitCapacityShortfallJson(string Day, decimal Missing, IReadOnlyList<string> RowIds);

public record UnitCapacityOptimizationJson(
    bool IsNeeded,
    IReadOnlyList<UnitCapacitySuggestionJson> Suggestions,
    IReadOnlyList<UnitCapacityShortfallJson> Shortfalls);

public static class UnitCapacityMapper
{
    public static UnitCapacityPreviewJson ToJson(this CapacityPlanPreviewDto dto, IHashids hashids)
        => new(
            dto.CurrentCapacity,
            dto.NewCapacity,
            dto.UnitArea,
            dto.MinimumUsageArea,
            dto.EffectiveNorm,
            dto.MaximumCapacity,
            dto.TotalAllocatedToday,
            dto.PeakAllocated,
            dto.PeakDay.ToString("dd.MM.yyyy"),
            dto.Rows.Select(row => new UnitCapacityRowJson(
                hashids.Encode(row.LeaseId),
                row.LeaseNo,
                row.TenantName,
                row.StartDate.ToString("dd.MM.yyyy"),
                row.EndDate.ToString("dd.MM.yyyy"),
                StatusLabel(row.Status),
                row.RentedArea,
                ModeLabel(row.CurrentMode),
                row.CurrentAllocatedArea,
                row.ProposedAllocatedArea,
                ModeLabel(row.ProposedMode),
                row.ProposedMode == LeaseAreaMode.Manual,
                row.Editable,
                row.Error)).ToList(),
            dto.Errors.Select(error => new UnitCapacityIssueJson(error.Code, error.Message, RowIdsOn(dto, error.Day, hashids))).ToList(),
            dto.Warnings.Select(warning => new UnitCapacityIssueJson(warning.Code, warning.Message, RowIdsOn(dto, warning.Day, hashids))).ToList(),
            dto.Hash,
            dto.HasErrors,
            dto.Errors.Any(error => error.Code == CapacityPlanCodes.CountExceeded),
            Math.Max(0, dto.NewCapacity - dto.Rows.Count(row => row.StartDate.Date <= dto.PeakDay.Date && dto.PeakDay.Date <= row.EndDate.Date)),
            ShortArea(dto, CapacityPlanCodes.NormTargetShort, warning => warning.NormTarget),
            dto.Warnings.Any(warning => warning.Code == CapacityPlanCodes.MinimumTargetShort),
            ShortArea(dto, CapacityPlanCodes.MinimumTargetShort, warning => warning.MinimumTarget),
            dto.Optimization == null
                ? null
                : new UnitCapacityOptimizationJson(
                    dto.Optimization.IsNeeded,
                    dto.Optimization.Suggestions
                        .Select(s => new UnitCapacitySuggestionJson(hashids.Encode(s.LeaseId), s.AllocatedArea)).ToList(),
                    dto.Optimization.Shortfalls
                        .Select(s => new UnitCapacityShortfallJson(
                            s.Day.ToString("dd.MM.yyyy"),
                            s.MissingArea,
                            RowIdsOn(dto, s.Day.Date > DateTime.Today ? s.Day : null, hashids))).ToList()));

    private static decimal? ShortArea(
        CapacityPlanPreviewDto dto, string code, Func<CapacityPlanWarningDto, decimal> target)
        => dto.Warnings.Where(warning => warning.Code == code)
            .Select(warning => (decimal?)Math.Max(0, target(warning) - warning.RemainingArea))
            .FirstOrDefault();

    private static IReadOnlyList<string> RowIdsOn(CapacityPlanPreviewDto dto, DateTime? day, IHashids hashids)
        => day is null
            ? []
            : dto.Rows.Where(row => row.StartDate.Date == day.Value.Date).Select(row => hashids.Encode(row.LeaseId)).ToList();

    private static string? ModeLabel(LeaseAreaMode? mode) => mode switch
    {
        LeaseAreaMode.Automatic => "Otomatik",
        LeaseAreaMode.Manual => "Elle girilen",
        _ => null
    };

    private static string StatusLabel(LeaseStatus status) => status switch
    {
        LeaseStatus.Active => "Aktif",
        LeaseStatus.Draft => "Taslak",
        LeaseStatus.RevisionRequested => "Revizyon İstendi",
        LeaseStatus.Ended => "Sona Erdi",
        LeaseStatus.Terminated => "Feshedildi",
        _ => status.ToString()
    };
}
