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
[Route("Admin/Kiracilar/{tenantId}/Kullanicilar")]
public class AdminTenantUserController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleService _userRolService;
    private readonly IInvitationService _davetiyeService;
    private readonly IAuditService _auditService;
    private readonly IPaymentLinkService _paymentLinkService;

    public AdminTenantUserController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IUserRoleService userRoleService,
        IInvitationService invitationService,
        IAuditService auditService,
        IPaymentLinkService paymentLinkService)
    {
        _db = db;
        _userManager = userManager;
        _userRolService = userRoleService;
        _davetiyeService = invitationService;
        _auditService = auditService;
        _paymentLinkService = paymentLinkService;
    }

    private async Task PopulateRollerAsync(List<RolSecenekViewModel> liste, int tenantId)
    {
        var roller = await _db.Roller.IgnoreQueryFilters()
            .Where(r => r.Scope == RoleScope.Tenant && (r.TenantId == null || r.TenantId == tenantId) && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync();
        liste.AddRange(roller.Select(r => new RolSecenekViewModel { Id = r.Id, Ad = r.Name }));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int tenantId)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == tenantId);
        if (tenant == null) return NotFound();

        var kullanicilar = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        var items = new List<KiraciKullaniciItem>();
        foreach (var u in kullanicilar)
        {
            var roller = await _db.UserRoller
                .Where(ur => ur.UserId == u.Id)
                .Join(_db.Roller, ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Name, r.Id })
                .FirstOrDefaultAsync();

            items.Add(new KiraciKullaniciItem
            {
                Id = u.Id,
                AdSoyad = u.AdSoyad ?? u.Email ?? "—",
                Email = u.Email ?? "—",
                RolAd = roller?.Name ?? "—",
                RolId = roller?.Id ?? 0,
                IsActive = u.IsActive
            });
        }

        var bekleyen = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .Where(d => d.TenantId == tenantId && d.Status == InvitationStatus.Pending)
            .Include(d => d.Role)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var davetItems = bekleyen.Select(d => new KiraciDavetItem
        {
            Id = d.Id,
            Email = d.Email,
            AdSoyad = d.FullName,
            RolAd = d.Role?.Name ?? "—",
            GonderimTarihi = d.CreatedAt,
            ExpiresAt = d.ExpiresAt
        }).ToList();

        ViewBag.TenantId = tenantId;
        ViewBag.KiraciAd = tenant.DisplayName;

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
    public async Task<IActionResult> ToggleActive(int tenantId, string id)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var eventType = user.IsActive ? "User.Activated" : "User.Deactivated";
        await _auditService.LogAsync(eventType, "ApplicationUser", user.Id, user.Email);

        TempData["Success"] = user.IsActive ? "Kullanıcı aktifleştirildi." : "Kullanıcı pasifleştirildi.";
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpPost("Davet/Iptal/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetIptal(int tenantId, int id)
    {
        var davetiye = await _db.Davetiyeler.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
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
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpPost("Davet/YenidenGonder/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DavetYenidenGonder(int tenantId, int id)
    {
        var davetiye = await _db.Davetiyeler.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
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
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpGet("Davet")]
    public async Task<IActionResult> Davet(int tenantId)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == tenantId);
        if (tenant == null) return NotFound();

        var model = new KiraciDavetViewModel();
        await PopulateRollerAsync(model.Roller, tenantId);
        model.Units = await GetKiraciBirimleriAsync(tenantId);

        ViewBag.TenantId = tenantId;
        ViewBag.KiraciAd = tenant.DisplayName;
        return View(model);
    }

    [HttpPost("Davet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Davet(int tenantId, KiraciDavetViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRollerAsync(model.Roller, tenantId);
            model.Units = await GetKiraciBirimleriAsync(tenantId);
            ViewBag.TenantId = tenantId;
            return View(model);
        }

        var rol = await _db.Roller.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == model.RolId && (r.TenantId == null || r.TenantId == tenantId));
        if (rol == null)
        {
            ModelState.AddModelError("RolId", "Geçersiz rol seçildi.");
            await PopulateRollerAsync(model.Roller, tenantId);
            model.Units = await GetKiraciBirimleriAsync(tenantId);
            ViewBag.TenantId = tenantId;
            return View(model);
        }

        try
        {
            var currentUserId = _userManager.GetUserId(User)!;
            var birimIds = model.BirimIds.Count > 0 ? model.BirimIds : null;
            await _davetiyeService.GonderAsync(model.Email, model.AdSoyad, model.RolId, currentUserId, tenantId, birimIds: birimIds);
            TempData["Success"] = $"{model.Email} adresine davet gönderildi.";
            return RedirectToAction(nameof(Index), new { tenantId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateRollerAsync(model.Roller, tenantId);
            model.Units = await GetKiraciBirimleriAsync(tenantId);
            ViewBag.TenantId = tenantId;
            return View(model);
        }
    }

    private async Task<List<UnitLookupDto>> GetKiraciBirimleriAsync(int tenantId)
    {
        return await _db.Leases
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == LeaseStatus.Active)
            .Select(s => new UnitLookupDto
            {
                Id = s.UnitId,
                Name = s.Unit.Name,
                PropertyName = s.Unit.Property.Name,
                UnitNo = s.Unit.UnitNo,
            })
            .Distinct()
            .OrderBy(b => b.PropertyName).ThenBy(b => b.Name)
            .ToListAsync();
    }

    [HttpPost("OdemeLink/Iptal/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OdemeLinkIptal(int tenantId, int id)
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
        return RedirectToAction(nameof(Index), new { tenantId });
    }
}
