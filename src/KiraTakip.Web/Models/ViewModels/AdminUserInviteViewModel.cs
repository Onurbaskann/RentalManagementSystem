namespace KiraTakip.Web.Models.ViewModels;

public class AdminUserInviteViewModel
{
    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public int RoleId { get; set; }

    public List<AdminUserRoleOptionViewModel> Roles { get; set; } = [];
    public bool HasAccessToAllSpaces { get; set; }
    public List<int> SelectedSpaceIds { get; set; } = [];
    public List<AdminUserSpaceOptionViewModel> Spaces { get; set; } = [];
}
