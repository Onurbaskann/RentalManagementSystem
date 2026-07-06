using KiraTakip.Authorization;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/RezervasyonTarifeKural")]
public class AdminRezervasyonTarifeKuralController : Controller
{
    private readonly IReservationService _service;
    private readonly IBirimRepository _birimRepo;

    public AdminRezervasyonTarifeKuralController(IReservationService service, IBirimRepository birimRepo)
    {
        _service = service;
        _birimRepo = birimRepo;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Index()
    {
        var liste = await _service.GetUcretKurallariAsync();
        return View(liste);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Create()
    {
        var vm = new RezervasyonTarifeKuralViewModel
        {
            FreeDurationMinutes = 120,
            UcretlendirmePeriyoduDakika = 60,
            KdvRate = 20,
            IsActive = true
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Create)]
    public async Task<IActionResult> Create(RezervasyonTarifeKuralViewModel vm)
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
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var kural = await _service.GetUcretKuralByIdAsync(id);
        if (kural == null) return NotFound();

        var vm = new RezervasyonTarifeKuralViewModel
        {
            Id = kural.Id,
            BirimId = kural.UnitId,
            FreeDurationMinutes = kural.FreeDurationMinutes,
            UcretlendirmePeriyoduDakika = kural.UcretlendirmePeriyoduDakika,
            PeriyotUcreti = kural.PeriyotUcreti,
            KdvRate = kural.KdvRate,
            IsActive = kural.IsActive,
            Aciklama = kural.Aciklama
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Edit)]
    public async Task<IActionResult> Edit(int id, RezervasyonTarifeKuralViewModel vm)
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
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        await _service.ToggleUcretKuralAktifAsync(id);
        TempData["Success"] = "Kural durumu değiştirildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(RezervasyonTarifeKuralViewModel vm)
    {
        vm.RezervasyonBirimleri = await _birimRepo.GetRezervasyonBirimleriAsync();
    }
}
