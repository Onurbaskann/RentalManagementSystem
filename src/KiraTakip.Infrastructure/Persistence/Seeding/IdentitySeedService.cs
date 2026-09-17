using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Identity;
using KiraTakip.Models.Dtos.Role;


namespace KiraTakip.Infrastructure.Seeding;

public class IdentitySeedService
{
    internal const string AdminEmail = "admin@kiratakip.local";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _rolService;

    public IdentitySeedService(
        UserManager<ApplicationUser> userManager,
        IRoleService roleService)
    {
        _userManager = userManager;
        _rolService = roleService;
    }

    public async Task SeedAsync()
    {
        // Süper Admin her ortamda seed'lenir — sisteme giriş noktası
        var admin = await EnsureUser(AdminEmail, "Admin123!", "Sistem Yöneticisi", tumTasinmazlaraErisim: true, isSuperAdmin: true);

        // Global Kiracı Yöneticisi rolünü seed et
        await _rolService.EnsureGlobalTenantRolesAsync(new EnsureGlobalTenantRolesInput(admin.Id));
    }


    private async Task<ApplicationUser> EnsureUser(string email, string password, string adSoyad, bool tumTasinmazlaraErisim = false, bool isSuperAdmin = false)
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
                IsActive = true,
                UserType = UserType.Internal,
                TumTasinmazlaraErisim = tumTasinmazlaraErisim,
                IsSuperAdmin = isSuperAdmin
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Seed admin oluşturulamadı: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        else
        {
            if (user.UserType != UserType.Internal)
                throw new InvalidOperationException("Seed admin hesabı iç kullanıcı olmalıdır.");
            var dirty = false;
            if (!user.IsActive) { user.IsActive = true; dirty = true; }
            if (user.TumTasinmazlaraErisim != tumTasinmazlaraErisim) { user.TumTasinmazlaraErisim = tumTasinmazlaraErisim; dirty = true; }
            if (user.IsSuperAdmin != isSuperAdmin) { user.IsSuperAdmin = isSuperAdmin; dirty = true; }
            if (dirty)
            {
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Seed admin güncellenemedi: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        return user;
    }
}
