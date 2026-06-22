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
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUserRolService _userRolService;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public IdentitySeedService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUserRolService userRolService,
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userRolService = userRolService;
        _db = db;
        _env = env;
    }

    public async Task SeedAsync()
    {
        string[] roleNames = [RoleNames.Admin, RoleNames.Yonetici, RoleNames.Goruntuleyici];

        // Identity şema uyumluluğu için AspNetRoles'u koru
        foreach (var roleName in roleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // Kendi Rol tablomuzu besle
        await EnsureRollerAsync(roleNames);

        // Admin her ortamda seed'lenir — sisteme giriş noktası
        await EnsureUser("admin@kiratakip.local", "Admin123!", RoleNames.Admin, "Admin Kullanıcı");

        // Test kullanıcıları sadece development'ta; production'da davet sistemi kullanılır
        if (_env.IsDevelopment())
        {
            await EnsureUser("yonetici@kiratakip.local", "Yonetici123!", RoleNames.Yonetici,      "Yönetici Kullanıcı",      tumTasinmazlaraErisim: true);
            await EnsureUser("viewer@kiratakip.local",   "Viewer123!",   RoleNames.Goruntuleyici, "Görüntüleyici Kullanıcı", tumTasinmazlaraErisim: false);
        }
    }

    private async Task EnsureRollerAsync(string[] roleNames)
    {
        foreach (var roleName in roleNames)
        {
            var isSystemRole = roleName == RoleNames.Admin;
            var existing = await _db.Roller.FirstOrDefaultAsync(r => r.Ad == roleName && r.Scope == RolScope.Internal);
            if (existing == null)
            {
                _db.Roller.Add(new Rol
                {
                    Ad = roleName,
                    Scope = RolScope.Internal,
                    IsSystemRole = isSystemRole,
                    IsActive = true,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (existing.IsSystemRole != isSystemRole)
            {
                existing.IsSystemRole = isSystemRole;
            }
        }
        await _db.SaveChangesAsync();

        await EnsureRolPermissionsAsync();
    }

    private async Task EnsureRolPermissionsAsync()
    {
        var yonetici = await _db.Roller.FirstOrDefaultAsync(r => r.Ad == RoleNames.Yonetici && r.Scope == RolScope.Internal);
        if (yonetici != null && !await _db.RolPermissions.AnyAsync(rp => rp.RolId == yonetici.Id))
        {
            foreach (var perm in PermissionCatalog.AssignableToYonetici)
                _db.RolPermissions.Add(new RolPermission { RolId = yonetici.Id, Permission = perm });
        }

        var goruntuleyici = await _db.Roller.FirstOrDefaultAsync(r => r.Ad == RoleNames.Goruntuleyici && r.Scope == RolScope.Internal);
        if (goruntuleyici != null && !await _db.RolPermissions.AnyAsync(rp => rp.RolId == goruntuleyici.Id))
        {
            foreach (var perm in PermissionCatalog.AssignableToGoruntuleyici)
                _db.RolPermissions.Add(new RolPermission { RolId = goruntuleyici.Id, Permission = perm });
        }

        await _db.SaveChangesAsync();
    }

    private async Task EnsureUser(string email, string password, string roleName, string adSoyad, bool tumTasinmazlaraErisim = false)
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
                TumTasinmazlaraErisim = tumTasinmazlaraErisim
            };
            await _userManager.CreateAsync(user, password);
        }
        else
        {
            var dirty = false;
            if (!user.IsActive) { user.IsActive = true; dirty = true; }
            if (user.TumTasinmazlaraErisim != tumTasinmazlaraErisim) { user.TumTasinmazlaraErisim = tumTasinmazlaraErisim; dirty = true; }
            if (dirty) await _userManager.UpdateAsync(user);
        }

        if (!await _userRolService.IsInRoleAsync(user.Id, roleName))
            await _userRolService.AddRoleByNameAsync(user.Id, roleName, "system");
    }
}
