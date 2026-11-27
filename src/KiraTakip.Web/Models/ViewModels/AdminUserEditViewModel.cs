namespace KiraTakip.Web.Models.ViewModels;

public class AdminUserEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public List<AdminUserRoleOptionViewModel> Roles { get; set; } = [];
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool HasAccessToAllSpaces { get; set; }
    public List<int> SelectedSpaceIds { get; set; } = [];
    public List<AdminUserSpaceOptionViewModel> Spaces { get; set; } = [];
}

public class AdminUserRoleOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AdminUserSpaceOptionViewModel
{
    public int SpaceId { get; set; }
    public string NamePath { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
