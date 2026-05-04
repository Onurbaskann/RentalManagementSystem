using System.Security.Claims;
using KiraTakip.Models;
using KiraTakip.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KiraTakip.Authorization;

public class PermissionClaimsTransformer : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly IPermissionService _permissionService;

    public PermissionClaimsTransformer(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IPermissionService permissionService)
        : base(userManager, roleManager, optionsAccessor)
    {
        _permissionService = permissionService;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var roles = await UserManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            foreach (var p in PermissionCatalog.All)
                identity.AddClaim(new Claim("permission", p));
        }
        else
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
            foreach (var p in permissions)
                identity.AddClaim(new Claim("permission", p));
        }

        return identity;
    }
}
