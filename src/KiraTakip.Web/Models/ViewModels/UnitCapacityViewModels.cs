using HashidsNet;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public class UnitCapacityPreviewRequest
{
    public int NewCapacity { get; set; }
    public Dictionary<string, decimal>? Overrides { get; set; }
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
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    string Hash,
    bool HasErrors);

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
            dto.Errors.Select(error => error.Message).ToList(),
            dto.Warnings.Select(warning => warning.Message).ToList(),
            dto.Hash,
            dto.HasErrors);

    private static string? ModeLabel(LeaseAreaMode? mode) => mode switch
    {
        LeaseAreaMode.Automatic => "Otomatik",
        LeaseAreaMode.Manual => "Manuel",
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
