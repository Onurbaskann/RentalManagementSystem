using System.Collections.Frozen;
using System.Security.Claims;
using KiraTakip.Services.Interfaces.Identity;

namespace KiraTakip.Web.Authorization;

public class CurrentUserPermissionContext : ICurrentUserPermissionContext
{
    private readonly IUserPermissionCache _permissionCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly object _lock = new();
    private readonly Dictionary<string, Task<IReadOnlySet<string>>> _userTasks = new(StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> EmptyPermissions =
        Array.Empty<string>().ToFrozenSet(StringComparer.Ordinal);

    public CurrentUserPermissionContext(
        IUserPermissionCache permissionCache,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissionCache = permissionCache;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IReadOnlySet<string>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(EmptyPermissions);
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(EmptyPermissions);
        }

        return GetPermissionsForUserAsync(userId, cancellationToken);
    }

    public Task<IReadOnlySet<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(EmptyPermissions);
        }

        lock (_lock)
        {
            if (_userTasks.TryGetValue(userId, out var existingTask))
            {
                return existingTask;
            }

            var task = _permissionCache.GetAsync(userId, cancellationToken);
            _userTasks[userId] = task;
            return task;
        }
    }
}
