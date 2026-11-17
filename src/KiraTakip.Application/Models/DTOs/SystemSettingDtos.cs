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
