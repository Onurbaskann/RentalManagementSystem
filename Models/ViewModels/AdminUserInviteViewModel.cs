namespace KiraTakip.Models.ViewModels;

public class AdminUserInviteViewModel
{
    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public int RoleId { get; set; }

    public List<AdminUserRoleOptionViewModel> Roles { get; set; } = [];
    public bool HasAccessToAllProperties { get; set; }
    public List<int> SelectedPropertyIds { get; set; } = [];
    public List<AdminUserPropertyOptionViewModel> Properties { get; set; } = [];
    public List<int> SelectedUnitIds { get; set; } = [];
    public List<AdminUserUnitOptionViewModel> Units { get; set; } = [];
}
