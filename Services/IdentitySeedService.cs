using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace KiraTakip.Services;

public class IdentitySeedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleService _userRolService;
    private readonly IRoleService _rolService;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public IdentitySeedService(
        UserManager<ApplicationUser> userManager,
        IUserRoleService userRoleService,
        IRoleService roleService,
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _userRolService = userRoleService;
        _rolService = roleService;
        _db = db;
        _env = env;
    }

    public async Task SeedAsync()
    {
        // Süper Admin her ortamda seed'lenir — sisteme giriş noktası
        await EnsureUser("admin@kiratakip.local", "Admin123!", null, "Sistem Yöneticisi", tumTasinmazlaraErisim: true, isSuperAdmin: true);

        // Global Kiracı Yöneticisi rolünü seed et
        await _rolService.EnsureGlobalKiraciRolleriAsync("system");
    }


    private async Task EnsureUser(string email, string password, string? roleName, string adSoyad, bool tumTasinmazlaraErisim = false, bool isSuperAdmin = false)
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
            await _userManager.CreateAsync(user, password);
        }
        else
        {
            var dirty = false;
            if (!user.IsActive) { user.IsActive = true; dirty = true; }
            if (user.TumTasinmazlaraErisim != tumTasinmazlaraErisim) { user.TumTasinmazlaraErisim = tumTasinmazlaraErisim; dirty = true; }
            if (user.IsSuperAdmin != isSuperAdmin) { user.IsSuperAdmin = isSuperAdmin; dirty = true; }
            if (dirty) await _userManager.UpdateAsync(user);
        }

        if (roleName != null && !await _userRolService.IsInRoleAsync(user.Id, roleName))
            await _userRolService.AddRoleByNameAsync(user.Id, roleName, "system");
    }
}
