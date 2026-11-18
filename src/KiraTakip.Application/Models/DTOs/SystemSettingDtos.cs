using KiraTakip.Models.Enums;
using KiraTakip.Models.Settings;

namespace KiraTakip.Models.Dtos;

public record SystemSettingListItemDto(
    int Id,
    string Key,
    string DisplayName,
    string GroupDisplayName,
    string Description,
    string Value,
    SystemSettingInputKind InputKind,
    bool IsEditable,
    decimal? MinimumValue,
    decimal? MaximumValue);

public record GetSystemSettingInput(int Id);

public record UpdateSystemSettingInput(int Id, string Value);

public record SystemSettingUpdateResult(IReadOnlyList<string> Warnings);

public record CapacityLeaseMinimumRow(
    string TenantName,
    LeaseStatus Status,
    decimal RentedArea,
    decimal AllocatedArea);

public record CapacityUnitMinimumRow(
    string PropertyName,
    string UnitName,
    decimal Area,
    int? Capacity,
    IReadOnlyList<CapacityLeaseMinimumRow> Leases);

public static class SystemSettingCodes
{
    public const string MinimumUsageAreaConflict = "Setting.MinimumUsageAreaConflict";
}
