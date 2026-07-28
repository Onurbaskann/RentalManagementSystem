using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IRoleService
{
    Task<List<Role>> GetInternalRolesAsync();
    Task<List<RoleListItemDto>> GetInternalRolesWithDetailsAsync();
    Task<Role?> GetRoleByIdAsync(GetRoleByIdInput input);
    Task<Role> CreateRoleAsync(CreateRoleInput input);
    Task UpdateRoleAsync(UpdateRoleInput input);
    Task DeleteRoleAsync(DeleteRoleInput input);
    Task<List<string>> GetRolePermissionsAsync(GetRolePermissionsInput input);
    Task SetRolePermissionsAsync(SetRolePermissionsInput input);
    Task<List<RoleListItemDto>> GetTenantRolesWithDetailsAsync(GetTenantRolesWithDetailsInput input);
    Task<TenantRoleEditDto> GetTenantRoleForEditAsync(GetTenantRoleForEditInput input);
    Task CreateTenantRoleAsync(CreateTenantRoleInput input);
    Task UpdateTenantRoleAsync(UpdateTenantRoleInput input);
    Task DeleteTenantRoleAsync(DeleteTenantRoleInput input);
    Task<int?> GetGlobalTenantManagerRoleIdAsync();
    Task EnsureGlobalTenantRolesAsync(EnsureGlobalTenantRolesInput input);
}
