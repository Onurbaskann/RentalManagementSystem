using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace KiraTakip.Authorization;

public class PermissionClaimsTransformer : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly IUserRolService _userRolService;

    public PermissionClaimsTransformer(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IUserRolService userRolService)
        : base(userManager, roleManager, optionsAccessor)
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
        if (roles.Contains(RoleNames.Admin))
        {
            foreach (var p in PermissionCatalog.All)
                identity.AddClaim(new Claim(AppClaimTypes.Permission, p));
        }
        else
        {
            var rolePerms = await _userRolService.GetUserPermissionsFromRolesAsync(user.Id);
            foreach (var p in rolePerms.Distinct())
                identity.AddClaim(new Claim(AppClaimTypes.Permission, p));
        }

        return identity;
    }
}
