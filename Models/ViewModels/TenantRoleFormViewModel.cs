namespace KiraTakip.Models.ViewModels;

public class TenantRoleFormViewModel
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> SelectedPermissions { get; set; } = [];
    public List<PermissionGroupViewModel> Permissions { get; set; } = [];
}
