using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        var userId = User.IsInRole("Goruntuleyici") ? _userManager.GetUserId(User) : null;
        var pagedResult = await _tahakkukService.GetPagedAsync(query, userId: userId);

        ViewBag.Tasinmazlar = await _ctx.Tasinmazlar.OrderBy(t => t.Ad).ToListAsync();
        ViewBag.Birimler = await _ctx.Birimler.OrderBy(b => b.TasinmazId).ThenBy(b => b.Ad).ToListAsync();
        ViewBag.Kiracilar = await _ctx.Kiraciler.OrderBy(k => k.Ad).ToListAsync();
        ViewBag.MevcutYillar = await _ctx.KiraTahakkuklar
            .Select(t => t.DonemBaslangic.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(pagedResult);
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> Detay(int id)
    {
        var tahakkuk = await _tahakkukService.GetByIdAsync(id);
        if (tahakkuk == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            if (!yetkiliIds.Contains(tahakkuk.KiraSozlesmesi.Birim.TasinmazId))
                return Forbid();
        }

        return View(tahakkuk);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    public async Task<IActionResult> Olustur()
    {
        var viewModel = new TahakkukOlusturViewModel();
        await PopulateSozlesmelerAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(TahakkukOlusturViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSozlesmelerAsync(viewModel);
            return View(viewModel);
        }

        var period = new DateTime(viewModel.DonemYil, viewModel.DonemAy, 1);
        var (isSuccess, errorMessage) = await _tahakkukService.OlusturAsync(viewModel.KiraSozlesmesiId, period);

        if (!isSuccess)
        {
            ModelState.AddModelError(string.Empty, errorMessage!);
            await PopulateSozlesmelerAsync(viewModel);
            return View(viewModel);
        }

        TempData["Success"] = $"{period:MMMM yyyy} dönemi için tahakkuk oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSozlesmelerAsync(TahakkukOlusturViewModel viewModel)
    {
        viewModel.AktifSozlesmeler = await _ctx.Sozlesmeler
            .Include(s => s.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(s => s.Kiraci)
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .OrderBy(s => s.Kiraci.Ad)
            .ToListAsync();
    }
}
