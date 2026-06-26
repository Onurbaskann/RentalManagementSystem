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
    private readonly IUserRolService _userRolService;
    private readonly IRolService _rolService;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public IdentitySeedService(
        UserManager<ApplicationUser> userManager,
        IUserRolService userRolService,
        IRolService rolService,
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _userRolService = userRolService;
        _rolService = rolService;
        _db = db;
        _env = env;
    }

    public async Task SeedAsync()
    {
        string[] roleNames = [RoleNames.SistemYoneticisi, RoleNames.OperasyonMuduru];

        await EnsureRollerAsync(roleNames);
        await _rolService.EnsureGlobalKiraciRolleriAsync("system");

        // Sistem Yöneticisi her ortamda seed'lenir — sisteme giriş noktası
        await EnsureUser("admin@kiratakip.local", "Admin123!", RoleNames.SistemYoneticisi, "Sistem Yöneticisi");

    }

    private async Task EnsureRollerAsync(string[] roleNames)
    {
        foreach (var roleName in roleNames)
        {
            var isSystemRole = true;
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
        var operasyonMuduru = await _db.Roller.FirstOrDefaultAsync(r => r.Ad == RoleNames.OperasyonMuduru && r.Scope == RolScope.Internal);
        if (operasyonMuduru != null && !await _db.RolPermissions.AnyAsync(rp => rp.RolId == operasyonMuduru.Id))
        {
            foreach (var perm in PermissionCatalog.OperasyonMuduruIzinleri)
                _db.RolPermissions.Add(new RolPermission { RolId = operasyonMuduru.Id, Permission = perm });
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
