using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Authorization;

public static class PermissionEvaluator
{
    private static readonly HashSet<string> Modules = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> ActionToModuleMap = new(StringComparer.Ordinal);

    static PermissionEvaluator()
    {
        foreach (var module in PermissionCatalog.AllModules)
        {
            Modules.Add(module.Path);
            foreach (var action in module.Actions)
            {
                ActionToModuleMap[action] = module.Path;
            }
        }
    }

    public static bool IsKnownPermission(string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        return Modules.Contains(permission) || ActionToModuleMap.ContainsKey(permission);
    }

    public static bool IsKnownModule(string? module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return false;

        return Modules.Contains(module);
    }

    public static bool IsSuperAdmin(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var userTypeStr = user.FindFirst(c => c.Type == AppClaimTypes.UserType)?.Value;
        if (string.IsNullOrWhiteSpace(userTypeStr) ||
            !int.TryParse(userTypeStr, out var userType) ||
            (UserType)userType != UserType.Internal)
        {
            return false;
        }

        return user.HasClaim(c => c.Type == "IsSuperAdmin" && string.Equals(c.Value, "true", StringComparison.Ordinal));
    }

    public static bool EvaluatePermission(IReadOnlySet<string>? userPermissions, string? permission, bool isSuperAdmin)
    {
        if (!IsKnownPermission(permission))
            return false;

        if (isSuperAdmin)
            return true;

        if (userPermissions == null || userPermissions.Count == 0)
            return false;

        if (Modules.Contains(permission!))
        {
            return userPermissions.Contains(permission!);
        }

        if (ActionToModuleMap.TryGetValue(permission!, out var parentModule))
        {
            return userPermissions.Contains(parentModule) && userPermissions.Contains(permission!);
        }

        return false;
    }

    public static bool EvaluateModuleAccess(IReadOnlySet<string>? userPermissions, string? module, bool isSuperAdmin)
    {
        if (!IsKnownModule(module))
            return false;

        if (isSuperAdmin)
            return true;

        if (userPermissions == null || userPermissions.Count == 0)
            return false;

        return userPermissions.Contains(module!);
    }
}