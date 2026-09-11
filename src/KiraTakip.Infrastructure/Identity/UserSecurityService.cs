using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Services.Identity;

public class UserSecurityService(
    UserManager<ApplicationUser> userManager,
    IUserRoleRepository userRoleRepository) : IUserSecurityService
{
    public async Task UpdateStampAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user != null)
            await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task UpdateStampForRoleUsersAsync(int rolId)
    {
        var userIds = await userRoleRepository.GetUserIdsByRoleIdAsync(rolId);

        foreach (var userId in userIds)
            await UpdateStampAsync(userId);
    }
}
