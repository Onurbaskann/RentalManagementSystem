namespace KiraTakip.Models.ViewModels;

public class PermissionGroupViewModel
{
    public string GroupName { get; set; } = string.Empty;
    public List<PermissionCheckboxViewModel> Permissions { get; set; } = [];
}
