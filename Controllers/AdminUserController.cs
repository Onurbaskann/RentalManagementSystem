using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "System.User")]
[Route("Admin/Kullanicilar")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPropertyService _tasinmazService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRoleService _userRolService;
    private readonly IInvitationService _invitationService;
    private readonly IAuditService _auditService;
    private readonly IPermissionScopeCache _kapsamCache;
    private readonly ApplicationDbContext _db;

    public AdminUserController(
        UserManager<ApplicationUser> userManager,
        IPropertyService propertyService,
        IPermissionService permissionService,
        IUserRoleService userRoleService,
        IInvitationService invitationService,
        IAuditService auditService,
        IPermissionScopeCache kapsamCache,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _tasinmazService = propertyService;
        _permissionService = permissionService;
        _userRolService = userRoleService;
        _invitationService = invitationService;
        _auditService = auditService;
        _kapsamCache = kapsamCache;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var icKullanicilar = await _userManager.Users
            .Where(u => u.TenantId == null && !u.IsSuperAdmin)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        var icItems = new List<KullaniciListeViewModel>();
        foreach (var u in icKullanicilar)
        {
            var roles = await _userRolService.GetUserRolesAsync(u.Id);
            icItems.Add(new KullaniciListeViewModel
            {
                Id = u.Id,
                AdSoyad = u.AdSoyad ?? u.Email ?? "—",
                Email = u.Email ?? "—",
                Rol = roles.FirstOrDefault() ?? "—",
                IsActive = u.IsActive
            });
        }

        var kiraciKullanicilar = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId != null)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        var kiraciItems = new List<KiraciKullaniciListItemViewModel>();
        foreach (var u in kiraciKullanicilar)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.Id == u.TenantId);
            var rol = await _db.UserRoller
                .Where(ur => ur.UserId == u.Id)
                .Join(_db.Roller, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .FirstOrDefaultAsync();

            kiraciItems.Add(new KiraciKullaniciListItemViewModel
            {
                Id = u.Id,
                AdSoyad = u.AdSoyad ?? u.Email ?? "—",
                Email = u.Email ?? "—",
                KiraciId = u.TenantId!.Value,
                KiraciAd = tenant?.DisplayName ?? "—",
                RolAd = rol ?? "—",
                IsActive = u.IsActive
            });
        }

        var vm = new AdminKullaniciIndexViewModel
        {
            IcKullanicilar = icItems,
            KiraciKullanicilar = kiraciItems,
            BekleyenDavetler = await _invitationService.GetBekleyenlerAsync()
        };

        return View(vm);
    }

    [HttpGet("Duzenle/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsSuperAdmin) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var yetkiliPropertyIds = await _db.KullaniciYetkiKapsamlari
            .Where(k => k.UserId == user.Id && k.ScopeType == ScopeType.Property && !k.IsDeleted)
            .Select(k => k.ScopeId)
            .ToListAsync();
        var yetkililBirimIds = await _db.KullaniciYetkiKapsamlari
            .Where(k => k.UserId == user.Id && k.ScopeType == ScopeType.Unit && !k.IsDeleted)
            .Select(k => k.ScopeId)
            .ToListAsync();
        var mevcutRolId = await _db.UserRoller
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => (int?)ur.RoleId)
            .FirstOrDefaultAsync() ?? 0;

        var model = new KullaniciDuzenleViewModel
        {
            Id = user.Id,
            AdSoyad = user.AdSoyad ?? string.Empty,
            Email = user.Email ?? string.Empty,
            RolId = mevcutRolId,
            IsActive = user.IsActive,
            IsCurrentUser = user.Id == currentUserId,
            TumTasinmazlaraErisim = user.TumTasinmazlaraErisim,
            SelectedTasinmazIds = yetkiliPropertyIds,
            SelectedBirimIds = yetkililBirimIds
        };

        await PopulateRollerAsync(model.Roller);
        await PopulateTasinmazlarAsync(model.Properties, yetkiliPropertyIds);
        await PopulateBirimlerAsync(model.Units, yetkililBirimIds);
        return View(model);
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, KullaniciDuzenleViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsSuperAdmin) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        model.IsCurrentUser = user.Id == currentUserId;

        if (model.RolId <= 0)
            ModelState.AddModelError("RolId", "Rol seçilmelidir.");

        if (!ModelState.IsValid)
        {
            await PopulateRollerAsync(model.Roller);
            await PopulateTasinmazlarAsync(model.Properties, model.SelectedTasinmazIds);
            await PopulateBirimlerAsync(model.Units, model.SelectedBirimIds);
            return View(model);
        }

        var yeniRol = await _db.Roller.FindAsync(model.RolId);
        if (yeniRol == null)
        {
            ModelState.AddModelError("RolId", "Geçersiz rol seçildi.");
            await PopulateRollerAsync(model.Roller);
            await PopulateTasinmazlarAsync(model.Properties, model.SelectedTasinmazIds);
            await PopulateBirimlerAsync(model.Units, model.SelectedBirimIds);
            return View(model);
        }

        var existingRoleNames = await _userRolService.GetUserRolesAsync(user.Id);
        var mevcutRolId = await _db.UserRoller
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => (int?)ur.RoleId)
            .FirstOrDefaultAsync() ?? 0;

        if (user.Id == currentUserId && mevcutRolId != model.RolId)
        {
            ModelState.AddModelError("RolId", "Kendi rolünüzü değiştiremezsiniz.");
            await PopulateRollerAsync(model.Roller);
            await PopulateTasinmazlarAsync(model.Properties, model.SelectedTasinmazIds);
            await PopulateBirimlerAsync(model.Units, model.SelectedBirimIds);
            return View(model);
        }

        // Skip admin count validation since Super Admin cannot be modified via UI

        await _userRolService.RemoveAllRolesAsync(user.Id);
        await _userRolService.AddRoleByRolIdAsync(user.Id, model.RolId, currentUserId);

        // İzinler artık rolden geliyor — per-user izin kaydı temizlenir
        await _permissionService.SetUserPermissionsAsync(user.Id, Array.Empty<string>());

        user.AdSoyad = model.AdSoyad;
        user.TumTasinmazlaraErisim = model.TumTasinmazlaraErisim;
        await _userManager.UpdateAsync(user);

        var tasinmazScopeIds = !model.TumTasinmazlaraErisim ? model.SelectedTasinmazIds : new List<int>();
        var birimScopeIds = !model.TumTasinmazlaraErisim ? model.SelectedBirimIds : new List<int>();
        await SetKapsamAsync(user.Id, tasinmazScopeIds, birimScopeIds, currentUserId ?? "system");

        TempData["Success"] = $"{user.AdSoyad ?? user.Email} kullanıcısı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsSuperAdmin) return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        var eventType = user.IsActive ? "User.Activated" : "User.Deactivated";
        await _auditService.LogAsync(eventType, "ApplicationUser", user.Id, user.Email);

        TempData["Success"] = $"{user.AdSoyad ?? user.Email} {(user.IsActive ? "aktif" : "pasif")} hale getirildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Davet")]
    public async Task<IActionResult> Davet()
    {
        var model = new DavetGonderViewModel();
        await PopulateDavetRollerAsync(model);
        await PopulateTasinmazlarAsync(model.Properties, new List<int>());
        await PopulateBirimlerAsync(model.Units, new List<int>());
        return View(model);
    }

    [HttpPost("Davet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Davet(DavetGonderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDavetRollerAsync(model);
            await PopulateTasinmazlarAsync(model.Properties, model.SelectedTasinmazIds);
            await PopulateBirimlerAsync(model.Units, model.SelectedBirimIds);
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Bu e-posta adresi sistemde kayıtlı bir kullanıcıya ait.");
            await PopulateDavetRollerAsync(model);
            await PopulateTasinmazlarAsync(model.Properties, model.SelectedTasinmazIds);
            await PopulateBirimlerAsync(model.Units, model.SelectedBirimIds);
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User)!;
        try
        {
            var tasinmazIds = model.TumTasinmazlaraErisim ? null : model.SelectedTasinmazIds;
            var birimIds = model.TumTasinmazlaraErisim ? null : model.SelectedBirimIds;
            await _invitationService.GonderAsync(model.Email, model.AdSoyad, model.RolId, currentUserId,
                tumTasinmazlaraErisim: model.TumTasinmazlaraErisim,
                tasinmazIds: tasinmazIds,
                birimIds: birimIds);

            TempData["Success"] = $"{model.Email} adresine davet gönderildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDavetRollerAsync(model);
            await PopulateTasinmazlarAsync(model.Properties, model.SelectedTasinmazIds);
            await PopulateBirimlerAsync(model.Units, model.SelectedBirimIds);
            return View(model);
        }
    }

    [HttpPost("DavetIptal/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetIptal(int id)
    {
        try
        {
            await _invitationService.IptalEtAsync(id);
            TempData["Success"] = "Davet iptal edildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DavetYenidenGonder/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetYenidenGonder(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        try
        {
            await _invitationService.YenidenGonderAsync(id, currentUserId);
            TempData["Success"] = "Davet yeniden gönderildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task SetKapsamAsync(string userId, List<int> tasinmazIds, List<int> birimIds, string atayan)
    {
        var mevcutlar = await _db.KullaniciYetkiKapsamlari
            .Where(k => k.UserId == userId && (k.ScopeType == ScopeType.Property || k.ScopeType == ScopeType.Unit))
            .ToListAsync();
        _db.KullaniciYetkiKapsamlari.RemoveRange(mevcutlar);

        foreach (var propertyId in tasinmazIds)
        {
            _db.KullaniciYetkiKapsamlari.Add(new UserPermissionScope
            {
                UserId = userId,
                ScopeType = ScopeType.Property,
                ScopeId = propertyId,
            });
        }

        foreach (var unitId in birimIds)
        {
            _db.KullaniciYetkiKapsamlari.Add(new UserPermissionScope
            {
                UserId = userId,
                ScopeType = ScopeType.Unit,
                ScopeId = unitId,
            });
        }

        await _db.SaveChangesAsync();
        _kapsamCache.Invalidate(userId);

        var parts = new List<string>();
        if (tasinmazIds.Count > 0) parts.Add($"{tasinmazIds.Count} taşınmaz");
        if (birimIds.Count > 0) parts.Add($"{birimIds.Count} unit");
        var detail = parts.Count > 0 ? $"Kapsam: {string.Join(", ", parts)}" : "Kapsam temizlendi";
        await _auditService.LogAsync("User.ScopeChanged", "ApplicationUser", userId, detail);
    }

        // Deleted AktifAdminSayisi method

    private async Task PopulateRollerAsync(List<RolSecenekViewModel> liste)
    {
        liste.Clear();
        var roller = await _db.Roller
            .Where(r => r.Scope == RoleScope.Internal && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.IsSystemRole ? 0 : 1).ThenBy(r => r.Name)
            .ToListAsync();
        liste.AddRange(roller.Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Name }));
    }

    private async Task PopulateDavetRollerAsync(DavetGonderViewModel model)
    {
        model.Roller = await _db.Roller
            .Where(r => r.Scope == RoleScope.Internal && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.IsSystemRole ? 0 : 1).ThenBy(r => r.Name)
            .Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Name })
            .ToListAsync();
    }

    private async Task PopulateTasinmazlarAsync(List<TasinmazYetkiCheckboxViewModel> liste, List<int> selectedIds)
    {
        liste.Clear();
        var tasinmazlar = await _tasinmazService.GetAllAsync();
        foreach (var t in tasinmazlar)
        {
            liste.Add(new TasinmazYetkiCheckboxViewModel
            {
                TasinmazId = t.Id,
                Ad = t.Ad,
                Konum = $"{t.Il} / {t.Ilce}",
                Selected = selectedIds?.Contains(t.Id) ?? false
            });
        }
    }

    private async Task PopulateBirimlerAsync(List<BirimYetkiCheckboxViewModel> liste, List<int> selectedIds)
    {
        liste.Clear();
        var birimler = await _db.Units.AsNoTracking()
            .OrderBy(b => b.Property.Name).ThenBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, TasinmazAd = b.Property.Name })
            .ToListAsync();
        foreach (var b in birimler)
        {
            liste.Add(new BirimYetkiCheckboxViewModel
            {
                BirimId = b.Id,
                Ad = b.Name,
                TasinmazAd = b.TasinmazAd,
                Selected = selectedIds?.Contains(b.Id) ?? false
            });
        }
    }
}
