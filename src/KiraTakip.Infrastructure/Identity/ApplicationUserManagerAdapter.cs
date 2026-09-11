using System.Linq;
using System.Threading.Tasks;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Infrastructure.Identity;

public class ApplicationUserManagerAdapter(UserManager<ApplicationUser> userManager) : IApplicationUserManager
{
    public Task<ApplicationUser?> FindByIdAsync(string userId)
        => userManager.FindByIdAsync(userId);

    public Task<ApplicationUser?> FindByEmailAsync(string email)
        => userManager.FindByEmailAsync(email);

    public string NormalizeEmail(string email)
        => userManager.NormalizeEmail(email)!;

    public async Task<AppIdentityResult> CreateAsync(ApplicationUser user, string? password = null)
    {
        var result = password is not null
            ? await userManager.CreateAsync(user, password)
            : await userManager.CreateAsync(user);

        return ToAppResult(result);
    }

    public async Task<AppIdentityResult> UpdateAsync(ApplicationUser user)
    {
        var result = await userManager.UpdateAsync(user);
        return ToAppResult(result);
    }

    public async Task<AppIdentityResult> UpdateSecurityStampAsync(ApplicationUser user)
    {
        var result = await userManager.UpdateSecurityStampAsync(user);
        return ToAppResult(result);
    }

    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        => userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<AppIdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
    {
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return ToAppResult(result);
    }

    private static AppIdentityResult ToAppResult(IdentityResult result)
    {
        return result.Succeeded
            ? AppIdentityResult.Success()
            : AppIdentityResult.Failed(result.Errors.Select(e => e.Description));
    }
}