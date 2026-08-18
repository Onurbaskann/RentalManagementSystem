using KiraTakip.Models.Settings;

namespace KiraTakip.Models.ViewModels;

public class SystemSettingEditViewModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string GroupDisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public SystemSettingInputKind InputKind { get; set; }
    public int? MinimumValue { get; set; }
    public int? MaximumValue { get; set; }
}
