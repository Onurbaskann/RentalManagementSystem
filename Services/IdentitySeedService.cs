using KiraTakip.Models;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Services;

public class IdentitySeedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public IdentitySeedService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        string[] roles = ["Admin", "Yonetici", "Goruntuleyici"];
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUser("admin@kiratakip.local", "Admin123!", "Admin", "Admin Kullanıcı");
        await EnsureUser("yonetici@kiratakip.local", "Yonetici123!", "Yonetici", "Yönetici Kullanıcı");
        await EnsureUser("viewer@kiratakip.local", "Viewer123!", "Goruntuleyici", "Görüntüleyici Kullanıcı");
    }

    private async Task EnsureUser(string email, string password, string role, string adSoyad)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                AdSoyad = adSoyad,
                EmailConfirmed = true,
                IsActive = true
            };
            await _userManager.CreateAsync(user, password);
        }
        else if (!user.IsActive)
        {
            user.IsActive = true;
            await _userManager.UpdateAsync(user);
        }

        if (!await _userManager.IsInRoleAsync(user, role))
            await _userManager.AddToRoleAsync(user, role);
    }
}
