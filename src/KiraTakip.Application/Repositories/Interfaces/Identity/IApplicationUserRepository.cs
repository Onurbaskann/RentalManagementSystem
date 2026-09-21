using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.AdminUser;
using KiraTakip.Models.Dtos.TenantUser;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Identity;

public interface IApplicationUserRepository : IRepository<ApplicationUser, string>
{
    Task<List<AdminUserAccountDto>> GetInternalAdminUsersAsync(CancellationToken ct = default);
    Task<List<AdminTenantUserAccountDto>> GetAdminTenantUsersAsync(CancellationToken ct = default);
    Task<PagedResult<AdminUserListItemDto>> GetInternalAdminUsersPageAsync(TableQuery query, CancellationToken ct = default);
    Task<PagedResult<AdminTenantUserListItemDto>> GetAdminTenantUsersPageAsync(TableQuery query, CancellationToken ct = default);
    Task<List<ApplicationUser>> GetUsersByTenantIdAsync(int tenantId, bool ignoreQueryFilters = false, CancellationToken ct = default);
    Task<List<TenantUserListItemDto>> GetTenantUserListAsync(int tenantId, CancellationToken ct = default);
    Task<PagedResult<TenantUserListItemDto>> GetTenantUserPageAsync(int tenantId, TableQuery query, CancellationToken ct = default);
    Task<TenantUserEditCoreDto?> GetTenantUserForEditAsync(string userId, int tenantId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);
    Task<UserScopeAccountDto?> GetScopeAccountAsync(string userId, CancellationToken ct = default);
    Task<ApplicationUser?> GetUserByIdAndTenantIdAsync(string userId, int tenantId, bool ignoreQueryFilters = false, CancellationToken ct = default);
    Task<bool> HasTenantManagerAsync(
        int tenantId,
        string? excludedUserId = null,
        int? excludedRoleId = null,
        CancellationToken ct = default);
    Task<Dictionary<string, string?>> GetDisplayNamesAsync(IReadOnlyCollection<string> userIds, CancellationToken ct = default);
    Task<string?> FindIdByNormalizedEmailForAuditAsync(string normalizedEmail, CancellationToken ct = default);
    Task<List<ApplicationUser>> GetByIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken ct = default);
}
