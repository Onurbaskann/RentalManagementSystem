using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IUserSecurityService _securityService;

    public RoleService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAuditService auditService, IUserSecurityService securityService)
    {
        _db = db;
        _userManager = userManager;
        _auditService = auditService;
        _securityService = securityService;
    }

    public Task<List<Role>> GetInternalRollerAsync()
        => _db.Roller
              .Where(r => r.Scope == RoleScope.Internal)
              .OrderBy(r => r.IsSystemRole ? 0 : 1)
              .ThenBy(r => r.Name)
              .ToListAsync();

    public Task<Role?> GetByIdAsync(int id)
        => _db.Roller.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Role> CreateAsync(string ad, string? aciklama, string createdBy)
    {
        if (await _db.Roller.AnyAsync(r => r.Name == ad && r.Scope == RoleScope.Internal && !r.IsDeleted))
            throw new InvalidOperationException($"'{ad}' adında bir rol zaten mevcut.");

        var rol = new Role
        {
            Name = ad,
            Description = aciklama,
            Scope = RoleScope.Internal,
            IsSystemRole = false,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.Roller.Add(rol);
        await _db.SaveChangesAsync();
        await _auditService.LogAsync("Role.Created", "Role", rol.Id.ToString(), ad);
        return rol;
    }

    public async Task UpdateAsync(int id, string ad, string? aciklama, string updatedBy)
    {
        var rol = await _db.Roller.FindAsync(id)
            ?? throw new InvalidOperationException("Rol bulunamadı.");

        if (!rol.IsSystemRole)
        {
            if (await _db.Roller.AnyAsync(r => r.Name == ad && r.Id != id && r.Scope == RoleScope.Internal && !r.IsDeleted))
                throw new InvalidOperationException($"'{ad}' adında bir rol zaten mevcut.");
            rol.Name = ad;
        }

        rol.Description = aciklama;
        await _db.SaveChangesAsync();
        await _auditService.LogAsync("Role.Updated", "Role", id.ToString(), ad);
    }

    public async Task SilAsync(int id, string deletedBy)
    {
        var rol = await _db.Roller.FindAsync(id)
            ?? throw new InvalidOperationException("Rol bulunamadı.");

        if (rol.IsSystemRole)
            throw new InvalidOperationException("Sistem rolleri silinemez.");

        if (await _db.UserRoller.AnyAsync(ur => ur.RoleId == id))
            throw new InvalidOperationException("Bu role atanmış kullanıcı var. Önce kullanıcıların rolünü değiştirin.");

        rol.IsDeleted = true;
        rol.IsActive = false;
        await _db.SaveChangesAsync();
        await _auditService.LogAsync("Role.Deleted", "Role", id.ToString(), rol.Name);
    }

    public Task<List<string>> GetRolPermissionsAsync(int rolId)
        => _db.RolPermissions
              .Where(rp => rp.RoleId == rolId)
              .Select(rp => rp.Permission)
              .ToListAsync();

    public async Task SetRolPermissionsAsync(int rolId, IEnumerable<string> permissions, string updatedBy)
    {
        var existing = await _db.RolPermissions.Where(rp => rp.RoleId == rolId).ToListAsync();
        _db.RolPermissions.RemoveRange(existing);

        var validPerms = permissions.Where(p => PermissionCatalog.All.Contains(p)).Distinct();
        foreach (var perm in validPerms)
            _db.RolPermissions.Add(new RolePermission { RoleId = rolId, Permission = perm });

        await _db.SaveChangesAsync();

        await _securityService.UpdateStampForRoleUsersAsync(rolId);

        await _auditService.LogAsync("Role.Permission.Changed", "Role", rolId.ToString(), updatedBy);
    }

    public Task<List<Role>> GetKiraciRollerAsync(int tenantId)
        => _db.Roller
              .Where(r => r.Scope == RoleScope.Tenant && (r.TenantId == null || r.TenantId == tenantId) && r.IsActive && !r.IsDeleted)
              .OrderBy(r => r.IsSystemRole ? 0 : 1)
              .ThenBy(r => r.Name)
              .ToListAsync();

    public async Task EnsureGlobalKiraciRolleriAsync(string createdBy)
    {
        var now = DateTime.UtcNow;

        var kiraciYonetici = await _db.Roller.FirstOrDefaultAsync(r => r.TenantId == null && r.Name == RoleNames.KiraciYoneticisi);
        if (kiraciYonetici == null)
        {
            kiraciYonetici = new Role
            {
                Name = RoleNames.KiraciYoneticisi,
                Scope = RoleScope.Tenant,
                TenantId = null,
                IsSystemRole = true,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = now
            };
            _db.Roller.Add(kiraciYonetici);
            await _db.SaveChangesAsync();
        }
        var mevcutKY = await _db.RolPermissions.Where(rp => rp.RoleId == kiraciYonetici.Id).ToListAsync();
        _db.RolPermissions.RemoveRange(mevcutKY);
        foreach (var perm in PermissionCatalog.TenantAll)
            _db.RolPermissions.Add(new RolePermission { RoleId = kiraciYonetici.Id, Permission = perm });

        await _db.SaveChangesAsync();
    }
}
