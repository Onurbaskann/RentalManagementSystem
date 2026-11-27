using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.Space;

namespace KiraTakip.Models.Dtos.AdminUser;

public record GetAdminUserIndexPageInput(TableQuery Query, bool TenantUsersTab);

public record GetAdminUserEditDataInput(string UserId, string? CurrentUserId);

public record UpdateAdminUserInput(
    string UserId,
    string? CurrentUserId,
    string FullName,
    int RoleId,
    bool HasAccessToAllSpaces,
    IReadOnlyList<int> SpaceIds);

public record ToggleAdminUserActiveInput(string UserId, string? CurrentUserId);

public record SendAdminUserInvitationInput(
    string Email,
    string? FullName,
    int RoleId,
    string InvitedByUserId,
    bool HasAccessToAllSpaces,
    IReadOnlyList<int> SpaceIds);

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

public record AdminUserFormOptionsDto(
    List<AdminUserRoleOptionDto> Roles,
    List<SpaceOptionDto> Spaces);

public record AdminUserEditDataDto(
    string Id,
    string FullName,
    string Email,
    int RoleId,
    bool IsActive,
    bool IsCurrentUser,
    bool HasAccessToAllSpaces,
    List<int> SelectedSpaceIds,
    AdminUserFormOptionsDto Options);

