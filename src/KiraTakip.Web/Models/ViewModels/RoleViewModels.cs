using System.Collections.Generic;

namespace KiraTakip.Web.Models.ViewModels;


public class RoleCreateViewModel
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> SelectedPermissions { get; set; } = [];
    public List<PermissionGroupViewModel> Permissions { get; set; } = [];
}

public class RoleEditViewModel
{
    public int Id { get; set; }
    public bool IsSystemRole { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> SelectedPermissions { get; set; } = [];
    public List<PermissionGroupViewModel> Permissions { get; set; } = [];
}
