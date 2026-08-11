using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record GetAdminUserIndexPageInput(TableQuery Query, bool TenantUsersTab);

public record GetAdminUserEditDataInput(string UserId, string? CurrentUserId);

public record UpdateAdminUserInput(
    string UserId,
    string? CurrentUserId,
    string FullName,
    int RoleId,
    bool HasAccessToAllProperties,
    IReadOnlyList<int> PropertyIds,
    IReadOnlyList<int> UnitIds);

public record ToggleAdminUserActiveInput(string UserId, string? CurrentUserId);

public record SendAdminUserInvitationInput(
    string Email,
    string? FullName,
    int RoleId,
    string InvitedByUserId,
    bool HasAccessToAllProperties,
    IReadOnlyList<int> PropertyIds,
    IReadOnlyList<int> UnitIds);

public record CancelAdminUserInvitationInput(int InvitationId);

public record ResendAdminUserInvitationInput(int InvitationId, string ResentByUserId);

public record AdminUserAccountDto(string Id, string? FullName, string? Email, bool IsActive);

public record AdminTenantUserAccountDto(
    string Id,
    string? FullName,
    string? Email,
    int TenantId,
    string? TenantName,
    bool IsActive);

public record AdminUserListItemDto(string Id, string FullName, string Email, string Role, bool IsActive);

public record AdminTenantUserListItemDto(
    string Id,
    string FullName,
    string Email,
    int TenantId,
    string TenantName,
    string RoleName,
    bool IsActive);

public record AdminPendingInvitationDto(int Id, string Email, string? FullName, DateTime ExpiresAt);

public record AdminUserIndexDto(
    List<AdminUserListItemDto> InternalUsers,
    List<AdminTenantUserListItemDto> TenantUsers,
    List<AdminPendingInvitationDto> PendingInvitations);

public record AdminUserIndexPageDto(
    PagedResult<AdminUserListItemDto> InternalUsers,
    PagedResult<AdminTenantUserListItemDto> TenantUsers,
    List<AdminPendingInvitationDto> PendingInvitations);

public record AdminUserRoleOptionDto(int Id, string Name);

public record AdminUserPropertyOptionDto(int Id, string Name, string Location);

public record AdminUserUnitOptionDto(int Id, string Name, string PropertyName);

public record AdminUserFormOptionsDto(
    List<AdminUserRoleOptionDto> Roles,
    List<AdminUserPropertyOptionDto> Properties,
    List<AdminUserUnitOptionDto> Units);

public record AdminUserEditDataDto(
    string Id,
    string FullName,
    string Email,
    int RoleId,
    bool IsActive,
    bool IsCurrentUser,
    bool HasAccessToAllProperties,
    List<int> SelectedPropertyIds,
    List<int> SelectedUnitIds,
    AdminUserFormOptionsDto Options);

