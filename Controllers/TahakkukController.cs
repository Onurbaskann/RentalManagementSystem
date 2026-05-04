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
public class TahakkukController : Controller
{
    private readonly ITahakkukService _tahakkukService;
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public TahakkukController(ITahakkukService tahakkukService, ApplicationDbContext ctx, UserManager<ApplicationUser> userManager)
    {
        _tahakkukService = tahakkukService;
        _ctx = ctx;
        _userManager = userManager;
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> Index(string? durum = null)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        var userId = User.IsInRole("Goruntuleyici") ? _userManager.GetUserId(User) : null;
        var tahakkuklar = await _tahakkukService.GetAllAsync(userId: userId);

        tahakkuklar = durum switch {
            "gecikti"  => tahakkuklar.Where(t => t.Durum == TahakkukDurumu.Gecikti).ToList(),
            "bekliyor" => tahakkuklar.Where(t => t.Durum == TahakkukDurumu.Bekleniyor).ToList(),
            _          => tahakkuklar
        };

        ViewBag.Durum = durum ?? "tum";
        return View(tahakkuklar);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    public async Task<IActionResult> Olustur()
    {
        var vm = new TahakkukOlusturViewModel();
        await PopulateSozlesmelerAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(TahakkukOlusturViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSozlesmelerAsync(vm);
            return View(vm);
        }

        var donem = new DateTime(vm.DonemYil, vm.DonemAy, 1);
        var (basarili, hata) = await _tahakkukService.OlusturAsync(vm.KiraSozlesmesiId, donem);

        if (!basarili)
        {
            ModelState.AddModelError(string.Empty, hata!);
            await PopulateSozlesmelerAsync(vm);
            return View(vm);
        }

        TempData["Success"] = $"{donem:MMMM yyyy} dönemi için tahakkuk oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSozlesmelerAsync(TahakkukOlusturViewModel vm)
    {
        vm.AktifSozlesmeler = await _ctx.Sozlesmeler
            .Include(s => s.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(s => s.Kiraci)
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .OrderBy(s => s.Kiraci.Ad)
            .ToListAsync();
    }
}
