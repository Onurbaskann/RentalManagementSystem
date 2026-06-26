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

[Authorize(Roles = RoleNames.SistemYoneticisi)]
[Route("Admin/Kullanicilar")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITasinmazService _tasinmazService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRolService _userRolService;
    private readonly IDavetiyeService _davetiyeService;
    private readonly IAuditService _auditService;
    private readonly IYetkiKapsamiCache _kapsamCache;
    private readonly ApplicationDbContext _db;

    public AdminUserController(
        UserManager<ApplicationUser> userManager,
        ITasinmazService tasinmazService,
        IPermissionService permissionService,
        IUserRolService userRolService,
        IDavetiyeService davetiyeService,
        IAuditService auditService,
        IYetkiKapsamiCache kapsamCache,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _tasinmazService = tasinmazService;
        _permissionService = permissionService;
        _userRolService = userRolService;
        _davetiyeService = davetiyeService;
        _auditService = auditService;
        _kapsamCache = kapsamCache;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var icKullanicilar = await _userManager.Users
            .Where(u => u.KiraciId == null)
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
            .Where(u => u.KiraciId != null)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        var kiraciItems = new List<KiraciKullaniciListItemViewModel>();
        foreach (var u in kiraciKullanicilar)
        {
            var kiraci = await _db.Kiraciler.IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.Id == u.KiraciId);
            var rol = await _db.UserRoller
                .Where(ur => ur.UserId == u.Id)
                .Join(_db.Roller, ur => ur.RolId, r => r.Id, (ur, r) => r.Ad)
                .FirstOrDefaultAsync();

            kiraciItems.Add(new KiraciKullaniciListItemViewModel
            {
                Id = u.Id,
                AdSoyad = u.AdSoyad ?? u.Email ?? "—",
                Email = u.Email ?? "—",
                KiraciId = u.KiraciId!.Value,
                KiraciAd = kiraci?.GosterimAdi ?? "—",
                RolAd = rol ?? "—",
                IsActive = u.IsActive
            });
        }

        var vm = new AdminKullaniciIndexViewModel
        {
            IcKullanicilar = icItems,
            KiraciKullanicilar = kiraciItems,
            BekleyenDavetler = await _davetiyeService.GetBekleyenlerAsync()
        };

        return View(vm);
    }

    [HttpGet("Duzenle/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var yetkiliIds = await _db.KullaniciYetkiKapsamlari
            .Where(k => k.UserId == user.Id && k.KapsamTipi == KapsamTipi.Tasinmaz && !k.IsDeleted)
            .Select(k => k.KapsamId)
            .ToListAsync();
        var mevcutRolId = await _db.UserRoller
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => (int?)ur.RolId)
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
            SelectedTasinmazIds = yetkiliIds
        };

        await PopulateRollerAsync(model.Roller);
        await PopulateTasinmazlarAsync(model.Tasinmazlar, yetkiliIds);
        return View(model);
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, KullaniciDuzenleViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        model.IsCurrentUser = user.Id == currentUserId;

        if (model.RolId <= 0)
            ModelState.AddModelError("RolId", "Rol seçilmelidir.");

        if (!ModelState.IsValid)
        {
            await PopulateRollerAsync(model.Roller);
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        var yeniRol = await _db.Roller.FindAsync(model.RolId);
        if (yeniRol == null)
        {
            ModelState.AddModelError("RolId", "Geçersiz rol seçildi.");
            await PopulateRollerAsync(model.Roller);
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        var existingRoleNames = await _userRolService.GetUserRolesAsync(user.Id);
        var mevcutRolId = await _db.UserRoller
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => (int?)ur.RolId)
            .FirstOrDefaultAsync() ?? 0;

        if (user.Id == currentUserId && mevcutRolId != model.RolId)
        {
            ModelState.AddModelError("RolId", "Kendi rolünüzü değiştiremezsiniz.");
            await PopulateRollerAsync(model.Roller);
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        if (existingRoleNames.Contains(RoleNames.SistemYoneticisi) && yeniRol.Ad != RoleNames.SistemYoneticisi)
        {
            if (await AktifAdminSayisi() <= 1)
            {
                ModelState.AddModelError("RolId", "Sistemde en az bir aktif Admin bulunmalıdır.");
                await PopulateRollerAsync(model.Roller);
                await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
                return View(model);
            }
        }

        await _userRolService.RemoveAllRolesAsync(user.Id);
        await _userRolService.AddRoleByRolIdAsync(user.Id, model.RolId, currentUserId);

        // İzinler artık rolden geliyor — per-user izin kaydı temizlenir
        await _permissionService.SetUserPermissionsAsync(user.Id, Array.Empty<string>());

        user.AdSoyad = model.AdSoyad;
        user.TumTasinmazlaraErisim = yeniRol.Ad != RoleNames.SistemYoneticisi && model.TumTasinmazlaraErisim;
        await _userManager.UpdateAsync(user);

        var scopeIds = (yeniRol.Ad == RoleNames.OperasyonMuduru && !model.TumTasinmazlaraErisim)
            ? model.SelectedTasinmazIds
            : new List<int>();
        await SetKapsamAsync(user.Id, scopeIds, currentUserId ?? "system");

        TempData["Success"] = $"{user.AdSoyad ?? user.Email} kullanıcısı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        if (user.IsActive)
        {
            var roles = await _userRolService.GetUserRolesAsync(user.Id);
            if (roles.Contains(RoleNames.SistemYoneticisi) && await AktifAdminSayisi() <= 1)
            {
                TempData["Error"] = "Sistemde en az bir aktif Admin bulunmalıdır.";
                return RedirectToAction(nameof(Index));
            }
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
        await PopulateTasinmazlarAsync(model.Tasinmazlar, new List<int>());
        return View(model);
    }

    [HttpPost("Davet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Davet(DavetGonderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDavetRollerAsync(model);
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User)!;
        try
        {
            await _davetiyeService.GonderAsync(model.Email, model.AdSoyad, model.RolId, currentUserId);

            var rol = await _db.Roller.FindAsync(model.RolId);
            if (rol?.Ad == RoleNames.OperasyonMuduru && model.SelectedTasinmazIds.Any())
            {
                var invitedUser = await _userManager.FindByEmailAsync(model.Email);
                if (invitedUser != null)
                    await SetKapsamAsync(invitedUser.Id, model.SelectedTasinmazIds, currentUserId);
            }

            TempData["Success"] = $"{model.Email} adresine davet gönderildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDavetRollerAsync(model);
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }
    }

    [HttpPost("DavetIptal/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetIptal(int id)
    {
        try
        {
            await _davetiyeService.IptalEtAsync(id);
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
            await _davetiyeService.YenidenGonderAsync(id, currentUserId);
            TempData["Success"] = "Davet yeniden gönderildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task SetKapsamAsync(string userId, List<int> tasinmazIds, string atayan)
    {
        var mevcutlar = await _db.KullaniciYetkiKapsamlari
            .Where(k => k.UserId == userId && k.KapsamTipi == KapsamTipi.Tasinmaz)
            .ToListAsync();
        _db.KullaniciYetkiKapsamlari.RemoveRange(mevcutlar);

        foreach (var tasinmazId in tasinmazIds)
        {
            _db.KullaniciYetkiKapsamlari.Add(new KullaniciYetkiKapsami
            {
                UserId = userId,
                KapsamTipi = KapsamTipi.Tasinmaz,
                KapsamId = tasinmazId,
            });
        }

        await _db.SaveChangesAsync();
        _kapsamCache.Invalidate(userId);

        var detail = tasinmazIds.Count == 0 ? "Kapsam temizlendi" : $"Kapsam: {tasinmazIds.Count} taşınmaz";
        await _auditService.LogAsync("User.ScopeChanged", "ApplicationUser", userId, detail);
    }

    private async Task<int> AktifAdminSayisi()
    {
        var admins = await _userRolService.GetUsersInRoleAsync(RoleNames.SistemYoneticisi);
        return admins.Count(u => u.IsActive);
    }

    private async Task PopulateRollerAsync(List<RolSecenekViewModel> liste)
    {
        liste.Clear();
        var roller = await _db.Roller
            .Where(r => r.Scope == RolScope.Internal && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.IsSystemRole ? 0 : 1).ThenBy(r => r.Ad)
            .ToListAsync();
        liste.AddRange(roller.Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Ad }));
    }

    private async Task PopulateDavetRollerAsync(DavetGonderViewModel model)
    {
        model.Roller = await _db.Roller
            .Where(r => r.Scope == RolScope.Internal && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.IsSystemRole ? 0 : 1).ThenBy(r => r.Ad)
            .Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Ad })
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
}
