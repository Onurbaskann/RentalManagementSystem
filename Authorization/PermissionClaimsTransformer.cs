using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace KiraTakip.Authorization;

public class PermissionClaimsTransformer : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly IUserRolService _userRolService;

    public PermissionClaimsTransformer(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        IUserRolService userRolService)
        : base(userManager, optionsAccessor)
    {
        _userRolService = userRolService;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // UserType ve KiraciId claim'leri (ICurrentUserContext için)
        identity.AddClaim(new Claim(AppClaimTypes.UserType, ((int)user.UserType).ToString()));
        if (user.KiraciId.HasValue)
            identity.AddClaim(new Claim(AppClaimTypes.KiraciId, user.KiraciId.Value.ToString()));

        // Rol claim'leri UserRol tablosundan (AspNetUserRoles kullanılmaz)
        var roles = await _userRolService.GetUserRolesAsync(user.Id);
        foreach (var roleName in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));

        // Permission claim'leri: Admin hariç izinler rol tanımından (RolPermissions) gelir
        if (roles.Contains(RoleNames.SistemYoneticisi))
        {
            foreach (var p in PermissionCatalog.All)
                identity.AddClaim(new Claim(AppClaimTypes.Permission, p));
        }
        else
        {
            var rolePerms = await _userRolService.GetUserPermissionsFromRolesAsync(user.Id);
            var expanded = ExpandWithImpliedViews(rolePerms.Distinct().ToHashSet());
            foreach (var p in expanded)
                identity.AddClaim(new Claim(AppClaimTypes.Permission, p));
        }

        return identity;
    }

    private static HashSet<string> ExpandWithImpliedViews(HashSet<string> permissions)
    {
        var allKnown = PermissionCatalog.All.Concat(PermissionCatalog.KiraciAll).ToHashSet();
        var toAdd = new List<string>();
        foreach (var perm in permissions)
        {
            if (perm.EndsWith(".View")) continue;
            var dot = perm.LastIndexOf('.');
            if (dot < 0) continue;
            var viewPerm = perm[..dot] + ".View";
            if (allKnown.Contains(viewPerm) && !permissions.Contains(viewPerm))
                toAdd.Add(viewPerm);
        }
        foreach (var v in toAdd) permissions.Add(v);
        return permissions;
    }
}
