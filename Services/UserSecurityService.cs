using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class UserSecurityService : IUserSecurityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UserSecurityService(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task UpdateStampAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
            await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task UpdateStampForRoleUsersAsync(int rolId)
    {
        var userIds = await _db.UserRoller
            .Where(ur => ur.RoleId == rolId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        foreach (var userId in userIds)
            await UpdateStampAsync(userId);
    }
}
