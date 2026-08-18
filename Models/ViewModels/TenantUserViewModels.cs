using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;

namespace KiraTakip.Models.ViewModels;

public class TenantUserListViewModel
{
    public PagedResult<TenantUserListItemViewModel> Users { get; set; } = new();
    public List<TenantInvitationListItemViewModel> PendingInvitations { get; set; } = [];
    public TableQuery Query { get; set; } = new();
    public bool CanInvite { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDeactivate { get; set; }
}

public class TenantUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class TenantInvitationListItemViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class TenantInvitationFormViewModel
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int RoleId { get; set; }
    public List<int> UnitIds { get; set; } = [];
    public List<RoleOptionViewModel> Roles { get; set; } = [];
    public List<UnitLookupDto> Units { get; set; } = [];
}

public class TenantUserEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public bool HasAccessToAllUnits { get; set; }
    public List<int> UnitIds { get; set; } = [];
    public List<RoleOptionViewModel> Roles { get; set; } = [];
    public List<UnitLookupDto> LeaseUnits { get; set; } = [];
    public List<UnitListItemDto> ReservableUnits { get; set; } = [];
}
