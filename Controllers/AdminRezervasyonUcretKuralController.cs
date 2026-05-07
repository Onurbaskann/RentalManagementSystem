using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.RezervasyonUcretKuralPerm.Manage)]
[Route("Admin/RezervasyonUcretKural")]
public class AdminRezervasyonUcretKuralController : Controller
{
    private readonly IRezervasyonService _service;
    private readonly ApplicationDbContext _ctx;

    public AdminRezervasyonUcretKuralController(IRezervasyonService service, ApplicationDbContext ctx)
    {
        _service = service;
        _ctx = ctx;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var liste = await _service.GetUcretKurallariAsync();
        return View(liste);
    }

    [HttpGet("Ekle")]
    public async Task<IActionResult> Create()
    {
        var vm = new RezervasyonUcretKuralViewModel
        {
            UcretsizSureDakika = 120,
            UcretlendirmePeriyoduDakika = 60,
            KdvOrani = 20,
            Aktif = true
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RezervasyonUcretKuralViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        var (basarili, hata, _) = await _service.SaveUcretKuralAsync(vm);
        if (!basarili)
        {
            ModelState.AddModelError(string.Empty, hata!);
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Ücret kuralı eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var kural = await _service.GetUcretKuralByIdAsync(id);
        if (kural == null) return NotFound();

        var vm = new RezervasyonUcretKuralViewModel
        {
            Id = kural.Id,
            BirimId = kural.BirimId,
            UcretsizSureDakika = kural.UcretsizSureDakika,
            UcretlendirmePeriyoduDakika = kural.UcretlendirmePeriyoduDakika,
            PeriyotUcreti = kural.PeriyotUcreti,
            KdvOrani = kural.KdvOrani,
            Aktif = kural.Aktif,
            Aciklama = kural.Aciklama
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RezervasyonUcretKuralViewModel vm)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        var (basarili, hata, _) = await _service.SaveUcretKuralAsync(vm);
        if (!basarili)
        {
            ModelState.AddModelError(string.Empty, hata!);
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Ücret kuralı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        await _service.ToggleUcretKuralAktifAsync(id);
        TempData["Success"] = "Kural durumu değiştirildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(RezervasyonUcretKuralViewModel vm)
    {
        vm.RezervasyonBirimleri = await _ctx.Birimler
            .Include(b => b.BirimTuru)
            .Include(b => b.Tasinmaz)
            .Where(b => b.BirimTuru != null && b.BirimTuru.RezervasyonYapilabilirMi)
            .OrderBy(b => b.Tasinmaz.Ad).ThenBy(b => b.Ad)
            .ToListAsync();
    }
}
