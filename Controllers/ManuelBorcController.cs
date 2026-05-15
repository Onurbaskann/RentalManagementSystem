using KiraTakip.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Controllers;

[Authorize]
public class ManuelBorcController : Controller
{
    private readonly IManuelBorcService _service;
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public ManuelBorcController(IManuelBorcService service, ApplicationDbContext ctx,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _ctx = ctx;
        _userManager = userManager;
    }

    [Authorize(Policy = PermissionCatalog.ManuelBorc.View)]
    public async Task<IActionResult> Index()
    {
        var userId = User.IsInRole(RoleNames.Goruntuleyici) ? _userManager.GetUserId(User) : null;
        var liste = await _service.GetAllAsync(userId);
        return View(liste);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.ManuelBorc.Create)]
    public async Task<IActionResult> Ekle(int? sozlesmeId)
    {
        var vm = new ManuelBorcCreateViewModel
        {
            VadeTarihi = DateTime.Today
        };
        if (sozlesmeId.HasValue)
            vm.SozlesmeId = sozlesmeId.Value;
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ManuelBorc.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(ManuelBorcCreateViewModel vm)
    {
        if (vm.SozlesmeId <= 0)
            ModelState.AddModelError("SozlesmeId", "Sözleşme seçilmelidir.");
        if (vm.BorcTipiId <= 0)
            ModelState.AddModelError("BorcTipiId", "Borç tipi seçilmelidir.");
        if (string.IsNullOrWhiteSpace(vm.Aciklama))
            ModelState.AddModelError("Aciklama", "Açıklama zorunludur.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var (basarili, hata, tahakkukId) = await _service.CreateAsync(vm, userId);

        if (!basarili)
        {
            ModelState.AddModelError(string.Empty, hata!);
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Manuel borç başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ManuelBorc.Cancel)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id, string neden)
    {
        if (string.IsNullOrWhiteSpace(neden))
        {
            TempData["Error"] = "İptal nedeni zorunludur.";
            return RedirectToAction(nameof(Index));
        }

        var userId = _userManager.GetUserId(User)!;
        var (basarili, hata) = await _service.CancelAsync(id, userId, neden);

        if (!basarili)
            TempData["Error"] = hata;
        else
            TempData["Success"] = "Manuel borç iptal edildi.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(ManuelBorcCreateViewModel vm)
    {
        vm.AktifSozlesmeler = await _ctx.Sozlesmeler
            .Include(s => s.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(s => s.Kiraci)
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .OrderBy(s => s.Kiraci.Ad)
            .ToListAsync();

        vm.BorcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && b.Davranis == BorcTipiDavranisi.KullaniciManuel)
            .OrderBy(b => b.Sira)
            .ToListAsync();
    }
}
