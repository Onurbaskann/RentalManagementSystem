using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[RequireKiraciId]
[Route("Kiraci/Kullanicilar")]
public class KiraciKullaniciController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRolService _userRolService;
    private readonly IDavetiyeService _davetiyeService;
    private readonly IKiraciKullaniciService _kullaniciService;
    private readonly IAuditService _auditService;

    public KiraciKullaniciController(
        ApplicationDbContext db,
        ICurrentUserContext currentUser,
        UserManager<ApplicationUser> userManager,
        IUserRolService userRolService,
        IDavetiyeService davetiyeService,
        IKiraciKullaniciService kullaniciService,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
        _userRolService = userRolService;
        _davetiyeService = davetiyeService;
        _kullaniciService = kullaniciService;
        _auditService = auditService;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Module)]
    public async Task<IActionResult> Index()
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var currentUserId = _userManager.GetUserId(User)!;

        var kullanicilar = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.KiraciId == kiraciId)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        var items = new List<KiraciKullaniciItem>();
        foreach (var u in kullanicilar)
        {
            var roller = await _db.UserRoller
                .Where(ur => ur.UserId == u.Id)
                .Join(_db.Roller, ur => ur.RolId, r => r.Id, (ur, r) => new { r.Ad, r.Id })
                .FirstOrDefaultAsync();

            items.Add(new KiraciKullaniciItem
            {
                Id = u.Id,
                AdSoyad = u.AdSoyad ?? u.Email ?? "—",
                Email = u.Email ?? "—",
                RolAd = roller?.Ad ?? "—",
                RolId = roller?.Id ?? 0,
                IsActive = u.IsActive,
                IsCurrentUser = u.Id == currentUserId
            });
        }

        var bekleyen = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .Where(d => d.KiraciId == kiraciId && d.Durum == DavetiyeDurum.Beklemede)
            .Include(d => d.Rol)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var davetItems = bekleyen.Select(d => new KiraciDavetItem
        {
            Id = d.Id,
            Email = d.Email,
            AdSoyad = d.AdSoyad,
            RolAd = d.Rol?.Ad ?? "—",
            GonderimTarihi = d.CreatedAt,
            ExpiresAt = d.ExpiresAt
        }).ToList();

        var canInvite = User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.KiraciPortal.System.Kullanici.Invite);
        var canManage = User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.KiraciPortal.System.Kullanici.Invite);

        return View(new KiraciKullaniciListeViewModel
        {
            Kullanicilar = items,
            BekleyenDavetler = davetItems,
            CanInvite = canInvite,
            CanManage = canManage
        });
    }

    [HttpGet("Davet")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Invite)]
    public async Task<IActionResult> Davet()
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var model = new KiraciDavetViewModel();
        await PopulateRollerAsync(model.Roller);
        model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
        return View(model);
    }

    [HttpPost("Davet")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Invite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Davet(KiraciDavetViewModel model)
    {
        var kiraciId = _currentUser.KiraciId!.Value;

        if (!ModelState.IsValid)
        {
            await PopulateRollerAsync(model.Roller);
            model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User)!;

        var rol = await _db.Roller.FirstOrDefaultAsync(r => r.Id == model.RolId && r.KiraciId == kiraciId && !r.IsSystemRole);
        if (rol == null)
        {
            ModelState.AddModelError("RolId", "Geçersiz rol seçildi.");
            await PopulateRollerAsync(model.Roller);
            model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
            return View(model);
        }

        try
        {
            var birimIds = model.BirimIds.Count > 0 ? model.BirimIds : null;
            await _davetiyeService.GonderAsync(model.Email, model.AdSoyad, model.RolId, currentUserId, kiraciId, birimIds: birimIds);
            TempData["Success"] = $"{model.Email} adresine davet gönderildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateRollerAsync(model.Roller);
            model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
            return View(model);
        }
    }

    private async Task<List<BirimLookupDto>> GetKiraciBirimleriAsync(int kiraciId)
    {
        return await _db.Sozlesmeler
            .AsNoTracking()
            .Where(s => s.KiraciId == kiraciId && s.Durum == SozlesmeDurumu.Aktif)
            .Select(s => new BirimLookupDto
            {
                Id = s.BirimId,
                Ad = s.Birim.Ad,
                TasinmazAd = s.Birim.Tasinmaz.Ad,
                BirimNo = s.Birim.BirimNo,
            })
            .Distinct()
            .OrderBy(b => b.TasinmazAd).ThenBy(b => b.Ad)
            .ToListAsync();
    }

    [HttpPost("Davet/Iptal/{id:int}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Invite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetIptal(int id)
    {
        var davetiye = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.KiraciId == _currentUser.KiraciId);

        if (davetiye == null) return NotFound();

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

    [HttpPost("Davet/YenidenGonder/{id:int}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Invite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetYenidenGonder(int id)
    {
        var davetiye = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.KiraciId == _currentUser.KiraciId);

        if (davetiye == null) return NotFound();

        try
        {
            var currentUserId = _userManager.GetUserId(User)!;
            await _davetiyeService.YenidenGonderAsync(id, currentUserId);
            TempData["Success"] = $"{davetiye.Email} adresine davet yeniden gönderildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Edit)]
    public async Task<IActionResult> Duzenle(string id)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.KiraciId == kiraciId);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;
        var mevcutRolId = await _db.UserRoller
            .Where(ur => ur.UserId == id)
            .Select(ur => (int?)ur.RolId)
            .FirstOrDefaultAsync() ?? 0;

        var model = new KiraciKullaniciDuzenleViewModel
        {
            Id = user.Id,
            AdSoyad = user.AdSoyad ?? string.Empty,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            IsCurrentUser = user.Id == currentUserId,
            RolId = mevcutRolId
        };

        await PopulateRollerAsync(model.Roller);
        return View(model);
    }

    [HttpPost("Duzenle/{id}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(string id, KiraciKullaniciDuzenleViewModel model)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.KiraciId == kiraciId);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;
        model.IsCurrentUser = user.Id == currentUserId;

        if (user.Id == currentUserId)
        {
            ModelState.AddModelError(string.Empty, "Kendi hesabınızı bu ekrandan değiştiremezsiniz.");
            await PopulateRollerAsync(model.Roller);
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            await PopulateRollerAsync(model.Roller);
            return View(model);
        }

        var yeniRol = await _db.Roller
            .FirstOrDefaultAsync(r => r.Id == model.RolId && r.KiraciId == kiraciId && !r.IsSystemRole);
        if (yeniRol == null)
        {
            ModelState.AddModelError("RolId", "Geçersiz rol seçildi.");
            await PopulateRollerAsync(model.Roller);
            return View(model);
        }

        try
        {
            await _kullaniciService.EnsureSonYetkiliAsync(kiraciId, excludeUserId: user.Id);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateRollerAsync(model.Roller);
            return View(model);
        }

        await _userRolService.RemoveAllRolesAsync(user.Id);
        await _userRolService.AddRoleByRolIdAsync(user.Id, model.RolId, currentUserId);

        user.AdSoyad = model.AdSoyad;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync("User.RoleChanged", "ApplicationUser", user.Id, $"KiraciId:{kiraciId}");
        TempData["Success"] = $"{user.AdSoyad ?? user.Email} güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Kullanici.Deactivate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.KiraciId == kiraciId);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;

        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        if (user.IsActive)
        {
            try
            {
                await _kullaniciService.EnsureSonYetkiliAsync(kiraciId, excludeUserId: user.Id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var eventType = user.IsActive ? "User.Activated" : "User.Deactivated";
        await _auditService.LogAsync(eventType, "ApplicationUser", user.Id, user.Email);

        TempData["Success"] = user.IsActive ? "Kullanıcı aktifleştirildi." : "Kullanıcı pasifleştirildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateRollerAsync(List<RolSecenekViewModel> target)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var roller = await _db.Roller
            .Where(r => r.KiraciId == kiraciId && !r.IsSystemRole && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Ad)
            .ToListAsync();

        target.AddRange(roller.Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Ad }));
    }
}
