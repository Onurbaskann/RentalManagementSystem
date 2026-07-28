using System.Collections.Generic;

namespace KiraTakip.Models.ViewModels;

public class RoleListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}

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
