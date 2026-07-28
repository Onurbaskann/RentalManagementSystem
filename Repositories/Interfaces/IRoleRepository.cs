using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IRoleRepository : IBaseRepository<Role>
{
    Task<List<AdminUserRoleOptionDto>> GetActiveInternalRoleOptionsAsync(CancellationToken ct = default);
    Task<Role?> GetActiveInternalByIdAsync(int roleId, CancellationToken ct = default);
    Task<List<Role>> GetActiveTenantRolesAsync(int tenantId, CancellationToken ct = default);
    Task<Role?> GetTenantRoleByIdAsync(int roleId, int tenantId, CancellationToken ct = default);
    Task<List<RoleListItemDto>> GetTenantRolesWithDetailsAsync(int tenantId, CancellationToken ct = default);
    Task<TenantRoleEditDto?> GetTenantRoleForEditAsync(int roleId, int tenantId, CancellationToken ct = default);
    Task<Role?> GetTenantOwnedByIdAsync(int roleId, int tenantId, CancellationToken ct = default);
    Task<Role?> GetActiveInternalByNameAsync(string roleName, CancellationToken ct = default);
}
