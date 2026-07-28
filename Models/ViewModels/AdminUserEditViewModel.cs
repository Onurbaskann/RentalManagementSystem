namespace KiraTakip.Models.ViewModels;

public class AdminUserEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public List<AdminUserRoleOptionViewModel> Roles { get; set; } = [];
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool HasAccessToAllProperties { get; set; }
    public List<int> SelectedPropertyIds { get; set; } = [];
    public List<AdminUserPropertyOptionViewModel> Properties { get; set; } = [];
    public List<int> SelectedUnitIds { get; set; } = [];
    public List<AdminUserUnitOptionViewModel> Units { get; set; } = [];
}

public class AdminUserRoleOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AdminUserPropertyOptionViewModel
{
    public int PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public class AdminUserUnitOptionViewModel
{
    public int UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
