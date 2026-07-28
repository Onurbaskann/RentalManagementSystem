using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

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
