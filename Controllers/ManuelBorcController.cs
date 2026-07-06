using KiraTakip.Authorization;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class ManuelBorcController : Controller
{
    private readonly IManuelBorcService _service;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IYetkiKapsamiProvider _provider;

    public ManuelBorcController(IManuelBorcService service, UserManager<ApplicationUser> userManager, IYetkiKapsamiProvider provider)
    {
        _service = service;
        _userManager = userManager;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.ManualCharge.Module)]
    public async Task<IActionResult> Index(string? durum, string? baglanti, int? sozlesmeId)
    {
        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        var birimIds = (!_provider.GlobalErisim && _provider.ErisilebilirBirimIds.Count > 0)
            ? _provider.ErisilebilirBirimIds : null;
        var liste = await _service.GetAllAsync(tasinmazIds, durum, baglanti, sozlesmeId, birimIds);
        ViewBag.IptalSayisi = await _service.GetIptalSayisiAsync(tasinmazIds, birimIds);
        ViewBag.Durum = durum ?? "tum";
        ViewBag.Baglanti = baglanti ?? "";
        ViewBag.SozlesmeId = sozlesmeId;
        return View(liste);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.ManualCharge.Create)]
    public async Task<IActionResult> Ekle(int? sozlesmeId)
    {
        var vm = new ManuelBorcCreateViewModel { DueDate = DateTime.Today };
        await PopulateDropdownsAsync(vm);
        if (sozlesmeId.HasValue)
        {
            vm.SozlesmeId = sozlesmeId.Value;
            var s = vm.AktifSozlesmeler.FirstOrDefault(x => x.Id == sozlesmeId.Value);
            if (s != null)
            {
                vm.KiraciId = s.KiraciId;
                vm.BirimId = s.BirimId;
            }
        }
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ManualCharge.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(ManuelBorcCreateViewModel vm)
    {
        if (vm.KiraciId <= 0)
            ModelState.AddModelError("KiraciId", "Kiracı seçilmelidir.");
        if (vm.BirimId <= 0)
            ModelState.AddModelError("BirimId", "Unit seçilmelidir.");
        if (vm.ChargeTypeId <= 0)
            ModelState.AddModelError("ChargeTypeId", "Borç tipi seçilmelidir.");
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
    [Authorize(Policy = PermissionCatalog.ManualCharge.Cancel)]
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
        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        vm.AktifSozlesmeler = await _service.GetAktifSozlesmelerAsync();
        vm.ChargeTypes = await _service.GetManuelBorcTipleriAsync();
        vm.Units = await _service.GetTumBirimlerAsync(tasinmazIds);
    }
}
