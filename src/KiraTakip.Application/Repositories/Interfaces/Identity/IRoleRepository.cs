using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.AdminUser;
using KiraTakip.Models.Dtos.Role;

namespace KiraTakip.Repositories.Interfaces.Identity;

public interface IRoleRepository : IRepositoryBase<Role>
{
    Task<List<AdminUserRoleOptionDto>> GetActiveInternalRoleOptionsAsync(CancellationToken ct = default);
    Task<Role?> GetActiveInternalByIdAsync(int roleId, CancellationToken ct = default);
    Task<List<Role>> GetActiveTenantRolesAsync(int tenantId, CancellationToken ct = default);
    Task<Role?> GetTenantRoleByIdAsync(int roleId, int tenantId, CancellationToken ct = default);
    Task<List<RoleListItemDto>> GetTenantRolesWithDetailsAsync(int tenantId, CancellationToken ct = default);
    Task<PagedResult<RoleListItemDto>> GetInternalRolesWithDetailsPagedAsync(TableQuery query, CancellationToken ct = default);
    Task<PagedResult<RoleListItemDto>> GetTenantRolesWithDetailsPagedAsync(int tenantId, TableQuery query, CancellationToken ct = default);
    Task<TenantRoleEditDto?> GetTenantRoleForEditAsync(int roleId, int tenantId, CancellationToken ct = default);
    Task<Role?> GetTenantOwnedByIdAsync(int roleId, int tenantId, CancellationToken ct = default);
    Task<Role?> GetActiveInternalByNameAsync(string roleName, CancellationToken ct = default);
}
