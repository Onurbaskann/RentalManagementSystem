using System.Security.Claims;

namespace KiraTakip.Web.Authorization;

public class CurrentUserPermissionService : ICurrentUserPermissionService
{
    private readonly ICurrentUserPermissionContext _permissionContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserPermissionService(
        ICurrentUserPermissionContext permissionContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissionContext = permissionContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<bool> HasPermissionAsync(string? permission, CancellationToken cancellationToken = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        return HasPermissionAsync(principal, permission, cancellationToken);
    }

    public Task<bool> HasModuleAccessAsync(string? module, CancellationToken cancellationToken = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        return HasModuleAccessAsync(principal, module, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal? user, string? permission, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(permission))
            return false;

        var targetUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(targetUserId))
            return false;

        var isSuperAdmin = PermissionEvaluator.IsSuperAdmin(user);
        if (isSuperAdmin)
        {
            return PermissionEvaluator.EvaluatePermission(null, permission, isSuperAdmin: true);
        }

        var currentRequestUser = _httpContextAccessor.HttpContext?.User;
        var currentUserId = currentRequestUser?.FindFirstValue(ClaimTypes.NameIdentifier);

        IReadOnlySet<string> permissions;
        if (string.Equals(currentUserId, targetUserId, StringComparison.Ordinal))
        {
            permissions = await _permissionContext.GetPermissionsAsync(cancellationToken);
        }
        else
        {
            permissions = await _permissionContext.GetPermissionsForUserAsync(targetUserId, cancellationToken);
        }

        return PermissionEvaluator.EvaluatePermission(permissions, permission, isSuperAdmin: false);
    }

    public async Task<bool> HasModuleAccessAsync(ClaimsPrincipal? user, string? module, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(module))
            return false;

        var targetUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(targetUserId))
            return false;

        var isSuperAdmin = PermissionEvaluator.IsSuperAdmin(user);
        if (isSuperAdmin)
        {
            return PermissionEvaluator.EvaluateModuleAccess(null, module, isSuperAdmin: true);
        }

        var currentRequestUser = _httpContextAccessor.HttpContext?.User;
        var currentUserId = currentRequestUser?.FindFirstValue(ClaimTypes.NameIdentifier);

        IReadOnlySet<string> permissions;
        if (string.Equals(currentUserId, targetUserId, StringComparison.Ordinal))
        {
            permissions = await _permissionContext.GetPermissionsAsync(cancellationToken);
        }
        else
        {
            permissions = await _permissionContext.GetPermissionsForUserAsync(targetUserId, cancellationToken);
        }

        return PermissionEvaluator.EvaluateModuleAccess(permissions, module, isSuperAdmin: false);
    }
}
