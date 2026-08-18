using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace KiraTakip.Authorization;

public class PermissionClaimsTransformer : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly IUserRoleService _userRolService;

    public PermissionClaimsTransformer(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        IUserRoleService userRoleService)
        : base(userManager, optionsAccessor)
    {
        _userRolService = userRoleService;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.AdSoyad))
            identity.AddClaim(new Claim(AppClaimTypes.DisplayName, user.AdSoyad));

        identity.AddClaim(new Claim(AppClaimTypes.UserType, ((int)user.UserType).ToString()));
        if (user.TenantId.HasValue)
            identity.AddClaim(new Claim(AppClaimTypes.TenantId, user.TenantId.Value.ToString()));

        var roles = await _userRolService.GetUserRolesAsync(user.Id);
        foreach (var roleName in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));

        if (user.IsSuperAdmin)
        {
            identity.AddClaim(new Claim("IsSuperAdmin", "true"));
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
