using System.Security.Claims;

namespace KiraTakip.Web.Authorization;

public interface ICurrentUserPermissionService
{
    Task<bool> HasPermissionAsync(string? permission, CancellationToken cancellationToken = default);
    Task<bool> HasModuleAccessAsync(string? module, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(ClaimsPrincipal? user, string? permission, CancellationToken cancellationToken = default);
    Task<bool> HasModuleAccessAsync(ClaimsPrincipal? user, string? module, CancellationToken cancellationToken = default);
}
