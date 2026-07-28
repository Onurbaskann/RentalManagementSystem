using KiraTakip.Models;

namespace KiraTakip.Models.Dtos;

public record GetTenantUsersListInput(int TenantId);

public record TenantUsersListDto(
    string TenantDisplayName,
    List<TenantUserListItemDto> Users,
    List<TenantInvitationListItemDto> PendingInvitations);

public record TenantUserListItemDto(
    string Id,
    string FullName,
    string Email,
    string RoleName,
    int RoleId,
    bool IsActive);

public record TenantInvitationListItemDto(
    int Id,
    string Email,
    string FullName,
    string RoleName,
    DateTime SentAt,
    DateTime ExpiresAt);

public record ToggleTenantUserActiveInput(int TenantId, string UserId, string CurrentUserId);

public record EnsureTenantManagerExistsInput(
    int TenantId,
    string? ExcludedUserId = null,
    int? ExcludedRoleId = null);

public record CancelTenantInvitationInput(int TenantId, int InvitationId);
public record ResendTenantInvitationInput(int TenantId, int InvitationId, string ResentByUserId);
public record GetInviteDataInput(int TenantId);

public record TenantInviteDataDto(
    string TenantDisplayName,
    List<RoleLookupDto> Roles,
    List<UnitLookupDto> Units);

public record RoleLookupDto(int Id, string Name);

public record SendTenantInvitationInput(
    int TenantId,
    string Email,
    string? FullName,
    int RoleId,
    string InvitedByUserId,
    List<int>? UnitIds);

public record SendInitialTenantRepresentativeInput(
    int TenantId,
    string Email,
    string? FullName,
    string InvitedByUserId);

public record InitialTenantInvitationResultDto(bool Sent, string? Error);

public record TenantUserEditCoreDto(
    string Id,
    string FullName,
    string Email,
    bool IsActive,
    int RoleId,
    string RoleName);

public record GetTenantUserForEditInput(
    int TenantId,
    string UserId,
    string CurrentUserId);

public record TenantUserEditDataDto(
    string Id,
    string FullName,
    string Email,
    bool IsActive,
    int RoleId,
    List<RoleLookupDto> Roles);

public record EditTenantUserInput(
    int TenantId,
    string UserId,
    string FullName,
    int RoleId,
    string CurrentUserId);