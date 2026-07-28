namespace KiraTakip.Models.Dtos;

public record GetRoleByIdInput(int Id);

public record CreateRoleInput(string Name, string? Description, string CreatedBy);

public record UpdateRoleInput(int Id, string Name, string? Description, string UpdatedBy);

public record DeleteRoleInput(int Id, string DeletedBy);

public record GetRolePermissionsInput(int RoleId);

public record SetRolePermissionsInput(int RoleId, IEnumerable<string> Permissions, string UpdatedBy);

public record GetTenantRolesWithDetailsInput(int TenantId);

public record GetTenantRoleForEditInput(int Id, int TenantId);

public record CreateTenantRoleInput(
    int TenantId,
    string Name,
    string? Description,
    IReadOnlyCollection<string> SelectedPermissions,
    string ActorUserId);

public record UpdateTenantRoleInput(
    int Id,
    int TenantId,
    string Name,
    string? Description,
    IReadOnlyCollection<string> SelectedPermissions,
    string ActorUserId);

public record DeleteTenantRoleInput(int Id, int TenantId, string ActorUserId);

public record TenantRoleEditDto(
    int Id,
    string Name,
    string? Description,
    List<string> SelectedPermissions);

public record EnsureGlobalTenantRolesInput(string CreatedBy);

public record RoleListItemDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    int UserCount,
    int PermissionCount
);
