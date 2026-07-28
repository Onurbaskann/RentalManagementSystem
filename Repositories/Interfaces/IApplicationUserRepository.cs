using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KiraTakip.Repositories.Interfaces;

public interface IApplicationUserRepository
{
    Task<List<AdminUserAccountDto>> GetInternalAdminUsersAsync(CancellationToken ct = default);
    Task<List<AdminTenantUserAccountDto>> GetAdminTenantUsersAsync(CancellationToken ct = default);
    Task<List<ApplicationUser>> GetUsersByTenantIdAsync(int tenantId, bool ignoreQueryFilters = false, CancellationToken ct = default);
    Task<List<TenantUserListItemDto>> GetTenantUserListAsync(int tenantId, CancellationToken ct = default);
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
    Task<List<ApplicationUser>> GetByIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken ct = default);
}
