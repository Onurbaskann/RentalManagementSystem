using KiraTakip.Models.Common;

namespace KiraTakip.Models.ViewModels;

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminTenantUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminPendingInvitationViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class AdminUserIndexViewModel
{
    public PagedResult<AdminUserListItemViewModel> InternalUsers { get; set; } = new();
    public PagedResult<AdminTenantUserListItemViewModel> TenantUsers { get; set; } = new();
    public List<AdminPendingInvitationViewModel> PendingInvitations { get; set; } = [];
    public TableQuery Query { get; set; } = new();
    public string ActiveTab { get; set; } = "ic";
}
