using KiraTakip.Authorization;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/ReservationRateOverride")]
public class AdminReservationRateRuleController : Controller
{
    private readonly IReservationService _service;
    private readonly IUnitRepository _birimRepo;

    public AdminReservationRateRuleController(IReservationService service, IUnitRepository birimRepo)
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
        var vm = new ReservationRateOverrideViewModel
        {
            FreeDurationMinutes = 120,
            BillingPeriodMinutes = 60,
            KdvRate = 20,
            IsActive = true
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Create)]
    public async Task<IActionResult> Create(ReservationRateOverrideViewModel vm)
    {
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

    [HttpGet("Duzenle/{id}")]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var kural = await _service.GetUcretKuralByIdAsync(id);
        if (kural == null) return NotFound();

        var vm = new ReservationRateOverrideViewModel
        {
            Id = kural.Id,
            UnitId = kural.UnitId,
            FreeDurationMinutes = kural.FreeDurationMinutes,
            BillingPeriodMinutes = kural.BillingPeriodMinutes,
            PeriodRate = kural.PeriodRate,
            KdvRate = kural.KdvRate,
            IsActive = kural.IsActive,
            Description = kural.Description
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] ReservationRateOverrideViewModel vm)
    {
        vm.Id = id;
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

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        await _service.ToggleUcretKuralAktifAsync(id);
        TempData["Success"] = "Kural durumu değiştirildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(ReservationRateOverrideViewModel vm)
    {
        vm.RezervasyonBirimleri = await _birimRepo.GetRezervasyonBirimleriAsync();
    }
}
