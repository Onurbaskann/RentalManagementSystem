using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class RolService : IRolService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IUserSecurityService _securityService;

    public RolService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAuditService auditService, IUserSecurityService securityService)
    {
        _db = db;
        _userManager = userManager;
        _auditService = auditService;
        _securityService = securityService;
    }

    public Task<List<Rol>> GetInternalRollerAsync()
        => _db.Roller
              .Where(r => r.Scope == RolScope.Internal)
              .OrderBy(r => r.IsSystemRole ? 0 : 1)
              .ThenBy(r => r.Ad)
              .ToListAsync();

    public Task<Rol?> GetByIdAsync(int id)
        => _db.Roller.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Rol> CreateAsync(string ad, string? aciklama, string createdBy)
    {
        if (await _db.Roller.AnyAsync(r => r.Ad == ad && r.Scope == RolScope.Internal && !r.IsDeleted))
            throw new InvalidOperationException($"'{ad}' adında bir rol zaten mevcut.");

        var rol = new Rol
        {
            Ad = ad,
            Aciklama = aciklama,
            Scope = RolScope.Internal,
            IsSystemRole = false,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.Roller.Add(rol);
        await _db.SaveChangesAsync();
        await _auditService.LogAsync("Role.Created", "Rol", rol.Id.ToString(), ad);
        return rol;
    }

    public async Task UpdateAsync(int id, string ad, string? aciklama, string updatedBy)
    {
        var rol = await _db.Roller.FindAsync(id)
            ?? throw new InvalidOperationException("Rol bulunamadı.");

        if (!rol.IsSystemRole)
        {
            if (await _db.Roller.AnyAsync(r => r.Ad == ad && r.Id != id && r.Scope == RolScope.Internal && !r.IsDeleted))
                throw new InvalidOperationException($"'{ad}' adında bir rol zaten mevcut.");
            rol.Ad = ad;
        }

        rol.Aciklama = aciklama;
        await _db.SaveChangesAsync();
        await _auditService.LogAsync("Role.Updated", "Rol", id.ToString(), ad);
    }

    public async Task SilAsync(int id, string deletedBy)
    {
        var rol = await _db.Roller.FindAsync(id)
            ?? throw new InvalidOperationException("Rol bulunamadı.");

        if (rol.IsSystemRole)
            throw new InvalidOperationException("Sistem rolleri silinemez.");

        if (await _db.UserRoller.AnyAsync(ur => ur.RolId == id))
            throw new InvalidOperationException("Bu role atanmış kullanıcı var. Önce kullanıcıların rolünü değiştirin.");

        rol.IsDeleted = true;
        rol.IsActive = false;
        await _db.SaveChangesAsync();
        await _auditService.LogAsync("Role.Deleted", "Rol", id.ToString(), rol.Ad);
    }

    public Task<List<string>> GetRolPermissionsAsync(int rolId)
        => _db.RolPermissions
              .Where(rp => rp.RolId == rolId)
              .Select(rp => rp.Permission)
              .ToListAsync();

    public async Task SetRolPermissionsAsync(int rolId, IEnumerable<string> permissions, string updatedBy)
    {
        var existing = await _db.RolPermissions.Where(rp => rp.RolId == rolId).ToListAsync();
        _db.RolPermissions.RemoveRange(existing);

        var validPerms = permissions.Where(p => PermissionCatalog.All.Contains(p)).Distinct();
        foreach (var perm in validPerms)
            _db.RolPermissions.Add(new RolPermission { RolId = rolId, Permission = perm });

        await _db.SaveChangesAsync();

        await _securityService.UpdateStampForRoleUsersAsync(rolId);

        await _auditService.LogAsync("Role.Permission.Changed", "Rol", rolId.ToString(), updatedBy);
    }

    public Task<List<Rol>> GetKiraciRollerAsync(int kiraciId)
        => _db.Roller
              .Where(r => r.Scope == RolScope.Kiraci && (r.KiraciId == null || r.KiraciId == kiraciId) && r.IsActive && !r.IsDeleted)
              .OrderBy(r => r.IsSystemRole ? 0 : 1)
              .ThenBy(r => r.Ad)
              .ToListAsync();

    public async Task EnsureGlobalKiraciRolleriAsync(string createdBy)
    {
        var now = DateTime.UtcNow;

        var kiraciYonetici = await _db.Roller.FirstOrDefaultAsync(r => r.KiraciId == null && r.Ad == RoleNames.KiraciYoneticisi);
        if (kiraciYonetici == null)
        {
            kiraciYonetici = new Rol
            {
                Ad = RoleNames.KiraciYoneticisi,
                Scope = RolScope.Kiraci,
                KiraciId = null,
                IsSystemRole = true,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = now
            };
            _db.Roller.Add(kiraciYonetici);
            await _db.SaveChangesAsync();
        }
        var mevcutKY = await _db.RolPermissions.Where(rp => rp.RolId == kiraciYonetici.Id).ToListAsync();
        _db.RolPermissions.RemoveRange(mevcutKY);
        foreach (var perm in PermissionCatalog.KiraciAll)
            _db.RolPermissions.Add(new RolPermission { RolId = kiraciYonetici.Id, Permission = perm });

        await _db.SaveChangesAsync();
    }
}
