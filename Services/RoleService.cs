using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RoleService(
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserRoleRepository userRoleRepository,
    IAuditService auditService,
    IUserSecurityService securityService,
    ITenantUserService tenantUserService,
    IUnitOfWork uow
) : IRoleService, ITransactionalService
{
    public Task<List<Role>> GetInternalRolesAsync()
        => roleRepository.GetAllAsync(r => r.Scope == RoleScope.Internal && !r.IsDeleted,
                                q => q.OrderBy(r => r.IsSystemRole ? 0 : 1).ThenBy(r => r.Name));

    public async Task<List<RoleListItemDto>> GetInternalRolesWithDetailsAsync()
    {
        var roller = await GetInternalRolesAsync();
        var list = new List<RoleListItemDto>();

        foreach (var r in roller)
        {
            var userCount = await userRoleRepository.CountUsersInRoleAsync(r.Id);
            var permissions = await rolePermissionRepository.GetPermissionsForRoleAsync(r.Id);
            list.Add(new RoleListItemDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.IsActive,
                userCount,
                permissions.Count
            ));
        }

        return list;
    }

    public Task<Role?> GetRoleByIdAsync(GetRoleByIdInput input)
        => roleRepository.GetAsync(r =>
            r.Id == input.Id &&
            r.Scope == RoleScope.Internal &&
            !r.IsDeleted);

    public async Task<Role> CreateRoleAsync(CreateRoleInput input)
    {
        Guard.InvalidField(
            await roleRepository.AnyAsync(r => r.Name == input.Name && r.Scope == RoleScope.Internal && !r.IsDeleted),
            nameof(input.Name),
            $"'{input.Name}' adında bir rol zaten mevcut.");

        var rol = new Role
        {
            Name = input.Name,
            Description = input.Description,
            Scope = RoleScope.Internal,
            IsSystemRole = false,
            IsActive = true,
            CreatedBy = input.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };
        await roleRepository.AddAsync(rol);
        await uow.SaveChangesAsync();
        await auditService.LogAsync("Role.Created", "Role", rol.Id.ToString(), input.Name);

        return rol;
    }

    public async Task UpdateRoleAsync(UpdateRoleInput input)
    {
        var rol = Guard.NotFound(
            await roleRepository.GetAsync(r =>
                r.Id == input.Id &&
                r.Scope == RoleScope.Internal &&
                !r.IsDeleted),
            "Rol bulunamadı.");

        if (!rol.IsSystemRole)
        {
            Guard.InvalidField(
                await roleRepository.AnyAsync(r =>
                    r.Name == input.Name &&
                    r.Id != input.Id &&
                    r.Scope == RoleScope.Internal &&
                    !r.IsDeleted),
                nameof(input.Name),
                $"'{input.Name}' adında bir rol zaten mevcut.");

            rol.Name = input.Name;
        }

        rol.Description = input.Description;

        await uow.SaveChangesAsync();
        await auditService.LogAsync("Role.Updated", "Role", input.Id.ToString(), rol.Name);
    }

    public async Task DeleteRoleAsync(DeleteRoleInput input)
    {
        var rol = Guard.NotFound(
            await roleRepository.GetAsync(r =>
                r.Id == input.Id &&
                r.Scope == RoleScope.Internal &&
                !r.IsDeleted),
            "Rol bulunamadı.");

        Guard.Conflict(rol.IsSystemRole, "Sistem rolleri silinemez.");

        Guard.Conflict(
            await userRoleRepository.HasAnyUsersInRoleAsync(input.Id),
            "Bu role atanmış kullanıcı var. Önce kullanıcıların rolünü değiştirin.");

        await roleRepository.DeleteAsync(input.Id);
        await uow.SaveChangesAsync();
        await auditService.LogAsync("Role.Deleted", "Role", input.Id.ToString(), rol.Name);
    }

    public Task<List<string>> GetRolePermissionsAsync(GetRolePermissionsInput input)
        => rolePermissionRepository.GetPermissionsForRoleAsync(input.RoleId);

    public async Task SetRolePermissionsAsync(SetRolePermissionsInput input)
    {
        var rol = Guard.NotFound(
            await roleRepository.GetAsync(r => r.Id == input.RoleId && !r.IsDeleted),
            "Rol bulunamadı.");

        var existing = await rolePermissionRepository.GetForRoleAsync(input.RoleId);
        await rolePermissionRepository.RemoveRangeAsync(existing);

        var allowedPermissions = rol.Scope == RoleScope.Internal
            ? PermissionCatalog.All
            : PermissionCatalog.TenantAll;
        var validPerms = input.Permissions.Where(allowedPermissions.Contains).Distinct();
        var toAdd = validPerms.Select(perm => new RolePermission { RoleId = input.RoleId, Permission = perm });

        await rolePermissionRepository.AddRangeAsync(toAdd);
        await uow.SaveChangesAsync();

        await securityService.UpdateStampForRoleUsersAsync(input.RoleId);

        await auditService.LogAsync("Role.Permission.Changed", "Role", input.RoleId.ToString(), input.UpdatedBy);
    }

    public Task<List<RoleListItemDto>> GetTenantRolesWithDetailsAsync(
        GetTenantRolesWithDetailsInput input)
        => roleRepository.GetTenantRolesWithDetailsAsync(input.TenantId);

    public async Task<TenantRoleEditDto> GetTenantRoleForEditAsync(
        GetTenantRoleForEditInput input)
        => Guard.NotFound(
            await roleRepository.GetTenantRoleForEditAsync(input.Id, input.TenantId),
            "Rol bulunamadı.",
            "TENANT_ROLE_NOT_FOUND");

    public async Task CreateTenantRoleAsync(CreateTenantRoleInput input)
    {
        EnsureValidTenantPermissions(input.SelectedPermissions);

        Guard.InvalidField(
            await roleRepository.AnyAsync(r => r.TenantId == input.TenantId && r.Name == input.Name && !r.IsDeleted),
            nameof(input.Name),
            $"'{input.Name}' adında bir rol zaten mevcut.",
            "TENANT_ROLE_NAME_CONFLICT");

        var role = new Role
        {
            Name = input.Name,
            Description = input.Description,
            Scope = RoleScope.Tenant,
            TenantId = input.TenantId,
            IsSystemRole = false,
            IsActive = true,
            CreatedBy = input.ActorUserId,
            CreatedAt = DateTime.UtcNow
        };
        await roleRepository.AddAsync(role);
        await uow.SaveChangesAsync();

        await ReplaceRolePermissionsAsync(role.Id, input.SelectedPermissions);
        await uow.SaveChangesAsync();

        await auditService.LogAsync("Role.Created", "Role", role.Id.ToString(), input.Name);
    }

    public async Task UpdateTenantRoleAsync(UpdateTenantRoleInput input)
    {
        EnsureValidTenantPermissions(input.SelectedPermissions);

        var role = Guard.NotFound(
            await roleRepository.GetTenantOwnedByIdAsync(input.Id, input.TenantId),
            "Rol bulunamadı.",
            "TENANT_ROLE_NOT_FOUND");

        Guard.Conflict(
            role.IsSystemRole,
            "Sistem rolleri düzenlenemez.",
            "TENANT_ROLE_SYSTEM_EDIT_FORBIDDEN");

        Guard.InvalidField(
            await roleRepository.AnyAsync(r =>
                r.TenantId == input.TenantId &&
                r.Name == input.Name &&
                r.Id != input.Id &&
                !r.IsDeleted),
            nameof(input.Name),
            $"'{input.Name}' adında bir rol zaten mevcut.",
            "TENANT_ROLE_NAME_CONFLICT");

        role.Name = input.Name;
        role.Description = input.Description;
        role.UpdatedBy = input.ActorUserId;
        role.UpdatedAt = DateTime.UtcNow;

        await ReplaceRolePermissionsAsync(role.Id, input.SelectedPermissions);

        await uow.SaveChangesAsync();
        await securityService.UpdateStampForRoleUsersAsync(input.Id);
        await auditService.LogAsync("Role.Updated", "Role", input.Id.ToString(), role.Name);
    }

    public async Task DeleteTenantRoleAsync(DeleteTenantRoleInput input)
    {
        var role = Guard.NotFound(
            await roleRepository.GetTenantOwnedByIdAsync(input.Id, input.TenantId),
            "Rol bulunamadı.",
            "TENANT_ROLE_NOT_FOUND");

        Guard.Conflict(
            role.IsSystemRole,
            "Sistem rolleri silinemez.",
            "TENANT_ROLE_SYSTEM_DELETE_FORBIDDEN");

        await tenantUserService.EnsureTenantManagerExistsAsync(
            new EnsureTenantManagerExistsInput(
                input.TenantId,
                ExcludedRoleId: input.Id));

        Guard.Conflict(
            await userRoleRepository.HasAnyUsersInRoleAsync(input.Id),
            "Bu role atanmış kullanıcı var. Önce kullanıcıların rolünü değiştirin.",
            "TENANT_ROLE_HAS_USERS");

        await roleRepository.DeleteAsync(input.Id);
        await uow.SaveChangesAsync();
        await auditService.LogAsync("Role.Deleted", "Role", input.Id.ToString(), role.Name);
    }

    private static void EnsureValidTenantPermissions(IReadOnlyCollection<string> permissions)
        => Guard.InvalidField(
            permissions.Any(permission => !PermissionCatalog.TenantAll.Contains(permission)),
            "SelectedPermissions",
            "Geçersiz izin seçimi.",
            "TENANT_ROLE_INVALID_PERMISSION");

    private async Task ReplaceRolePermissionsAsync(
        int roleId,
        IReadOnlyCollection<string> permissions)
    {
        var existing = await rolePermissionRepository.GetForRoleAsync(roleId);
        await rolePermissionRepository.RemoveRangeAsync(existing);

        var replacements = permissions
            .Distinct()
            .Select(permission => new RolePermission
            {
                RoleId = roleId,
                Permission = permission
            });
        await rolePermissionRepository.AddRangeAsync(replacements);
    }

    public async Task<int?> GetGlobalTenantManagerRoleIdAsync()
    {
        var role = await roleRepository.GetAsync(item =>
            item.TenantId == null && item.Name == RoleNames.KiraciYoneticisi);

        return role?.Id;
    }

    public async Task EnsureGlobalTenantRolesAsync(EnsureGlobalTenantRolesInput input)
    {
        var now = DateTime.UtcNow;

        var kiraciYonetici = await roleRepository.GetAsync(r => r.TenantId == null && r.Name == RoleNames.KiraciYoneticisi);
        if (kiraciYonetici == null)
        {
            kiraciYonetici = new Role
            {
                Name = RoleNames.KiraciYoneticisi,
                Scope = RoleScope.Tenant,
                TenantId = null,
                IsSystemRole = true,
                IsActive = true,
                CreatedBy = input.CreatedBy,
                CreatedAt = now
            };
            await roleRepository.AddAsync(kiraciYonetici);
            await uow.SaveChangesAsync();
        }

        var mevcutKY = await rolePermissionRepository.GetForRoleAsync(kiraciYonetici.Id);
        await rolePermissionRepository.RemoveRangeAsync(mevcutKY);

        var toAdd = PermissionCatalog.TenantAll.Select(perm => new RolePermission { RoleId = kiraciYonetici.Id, Permission = perm });
        await rolePermissionRepository.AddRangeAsync(toAdd);

        await uow.SaveChangesAsync();
    }
}
