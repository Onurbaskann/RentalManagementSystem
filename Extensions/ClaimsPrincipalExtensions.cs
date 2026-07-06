using System;
using System.Linq;
using System.Security.Claims;

namespace KiraTakip.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool HasModuleAccess(this ClaimsPrincipal user, string modulePrefix)
    {
        // 1. Süper admin her modüle erişebilir
        if (user.HasClaim(c => c.Type == "IsSuperAdmin" && c.Value == "true"))
            return true;
            
        // 2. Kullanıcının claim'leri arasında bu prefix ile başlayan bir Permission var mı?
        // Örn: "Internal.Property" prefix'i verildiğinde, "Internal.Property.Create" varsa true döner.
        return user.Claims.Any(c => 
            c.Type == KiraTakip.Authorization.AppClaimTypes.Permission && 
            c.Value.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        if (user.HasClaim(c => c.Type == "IsSuperAdmin" && c.Value == "true"))
            return true;
            
        return user.HasClaim(KiraTakip.Authorization.AppClaimTypes.Permission, permission);
    }
}
