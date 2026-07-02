using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "System.User")]
[Route("Admin/Kiracilar/{kiraciId:int}/Kullanicilar")]
public class AdminKiraciKullaniciController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRolService _userRolService;
    private readonly IDavetiyeService _davetiyeService;
    private readonly IAuditService _auditService;
    private readonly IPaymentLinkService _paymentLinkService;

    public AdminKiraciKullaniciController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IUserRolService userRolService,
        IDavetiyeService davetiyeService,
        IAuditService auditService,
        IPaymentLinkService paymentLinkService)
    {
        _db = db;
        _userManager = userManager;
        _userRolService = userRolService;
        _davetiyeService = davetiyeService;
        _auditService = auditService;
        _paymentLinkService = paymentLinkService;
    }

    private async Task PopulateRollerAsync(List<RolSecenekViewModel> liste, int kiraciId)
    {
        var roller = await _db.Roller.IgnoreQueryFilters()
            .Where(r => r.Scope == RoleScope.Tenant && (r.KiraciId == null || r.KiraciId == kiraciId) && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Ad)
            .ToListAsync();
        liste.AddRange(roller.Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Ad }));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int kiraciId)
    {
        var kiraci = await _db.Kiraciler.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == kiraciId);
        if (kiraci == null) return NotFound();

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
                IsActive = u.IsActive
            });
        }

        var bekleyen = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .Where(d => d.KiraciId == kiraciId && d.Durum == InvitationStatus.Pending)
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

        ViewBag.KiraciId = kiraciId;
        ViewBag.KiraciAd = kiraci.GosterimAdi;

        return View(new KiraciKullaniciListeViewModel
        {
            Kullanicilar = items,
            BekleyenDavetler = davetItems,
            CanInvite = true,
            CanManage = true
        });
    }

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int kiraciId, string id)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.KiraciId == kiraciId);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var eventType = user.IsActive ? "User.Activated" : "User.Deactivated";
        await _auditService.LogAsync(eventType, "ApplicationUser", user.Id, user.Email);

        TempData["Success"] = user.IsActive ? "Kullanıcı aktifleştirildi." : "Kullanıcı pasifleştirildi.";
        return RedirectToAction(nameof(Index), new { kiraciId });
    }

    [HttpPost("Davet/Iptal/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetIptal(int kiraciId, int id)
    {
        var davetiye = await _db.Davetiyeler.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.KiraciId == kiraciId);
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
        return RedirectToAction(nameof(Index), new { kiraciId });
    }

    [HttpPost("Davet/YenidenGonder/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetYenidenGonder(int kiraciId, int id)
    {
        var davetiye = await _db.Davetiyeler.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.KiraciId == kiraciId);
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
        return RedirectToAction(nameof(Index), new { kiraciId });
    }

    [HttpGet("Davet")]
    public async Task<IActionResult> Davet(int kiraciId)
    {
        var kiraci = await _db.Kiraciler.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == kiraciId);
        if (kiraci == null) return NotFound();

        var model = new KiraciDavetViewModel();
        await PopulateRollerAsync(model.Roller, kiraciId);
        model.Birimler = await GetKiraciBirimleriAsync(kiraciId);

        ViewBag.KiraciId = kiraciId;
        ViewBag.KiraciAd = kiraci.GosterimAdi;
        return View(model);
    }

    [HttpPost("Davet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Davet(int kiraciId, KiraciDavetViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRollerAsync(model.Roller, kiraciId);
            model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
            ViewBag.KiraciId = kiraciId;
            return View(model);
        }

        var rol = await _db.Roller.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == model.RolId && (r.KiraciId == null || r.KiraciId == kiraciId));
        if (rol == null)
        {
            ModelState.AddModelError("RolId", "Geçersiz rol seçildi.");
            await PopulateRollerAsync(model.Roller, kiraciId);
            model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
            ViewBag.KiraciId = kiraciId;
            return View(model);
        }

        try
        {
            var currentUserId = _userManager.GetUserId(User)!;
            var birimIds = model.BirimIds.Count > 0 ? model.BirimIds : null;
            await _davetiyeService.GonderAsync(model.Email, model.AdSoyad, model.RolId, currentUserId, kiraciId, birimIds: birimIds);
            TempData["Success"] = $"{model.Email} adresine davet gönderildi.";
            return RedirectToAction(nameof(Index), new { kiraciId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateRollerAsync(model.Roller, kiraciId);
            model.Birimler = await GetKiraciBirimleriAsync(kiraciId);
            ViewBag.KiraciId = kiraciId;
            return View(model);
        }
    }

    private async Task<List<BirimLookupDto>> GetKiraciBirimleriAsync(int kiraciId)
    {
        return await _db.Sozlesmeler
            .AsNoTracking()
            .Where(s => s.KiraciId == kiraciId && s.Durum == LeaseStatus.Active)
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

    [HttpPost("OdemeLink/Iptal/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OdemeLinkIptal(int kiraciId, int id)
    {
        try
        {
            var iptalEdenUserId = _userManager.GetUserId(User)!;
            await _paymentLinkService.IptalEtAsync(id, iptalEdenUserId);
            TempData["Success"] = "Ödeme linki iptal edildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { kiraciId });
    }
}
