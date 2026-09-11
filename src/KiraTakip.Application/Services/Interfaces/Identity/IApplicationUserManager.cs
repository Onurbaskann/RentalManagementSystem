using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces.Identity;

public interface IApplicationUserManager
{
    Task<ApplicationUser?> FindByIdAsync(string userId);
    Task<ApplicationUser?> FindByEmailAsync(string email);
    string NormalizeEmail(string email);
    Task<AppIdentityResult> CreateAsync(ApplicationUser user, string? password = null);
    Task<AppIdentityResult> UpdateAsync(ApplicationUser user);
    Task<AppIdentityResult> UpdateSecurityStampAsync(ApplicationUser user);
    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
    Task<AppIdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
}