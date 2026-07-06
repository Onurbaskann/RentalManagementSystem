using KiraTakip.Authorization;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class RezervasyonController : Controller
{
    private readonly IReservationService _service;
    private readonly IBirimRepository _birimRepo;
    private readonly IKiraciRepository _kiraciRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IYetkiKapsamiProvider _provider;

    public RezervasyonController(
        IReservationService service,
        IBirimRepository birimRepo,
        IKiraciRepository kiraciRepo,
        UserManager<ApplicationUser> userManager,
        IYetkiKapsamiProvider provider)
    {
        _service = service;
        _birimRepo = birimRepo;
        _kiraciRepo = kiraciRepo;
        _userManager = userManager;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Index()
    {
        var liste = await _service.GetAllAsync(_provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds);
        return View(liste);
    }

    [HttpGet("Reservation/Detay/{id:int}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var rezervasyon = await _service.GetByIdAsync(id);
        if (rezervasyon == null) return NotFound();

        if (!_provider.GlobalErisim &&
            !_provider.ErisilebilirTasinmazIds.Contains(rezervasyon.TasinmazId))
            return Forbid();

        return View(rezervasyon);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Reservation.Create)]
    public async Task<IActionResult> Ekle(int? birimId)
    {
        var vm = new RezervasyonCreateViewModel
        {
            BirimId = birimId,
            StartDate = DateTime.Today.AddHours(9),
            EndDate = DateTime.Today.AddHours(11)
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Reservation.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(RezervasyonCreateViewModel vm)
    {
        if (!vm.BirimId.HasValue || vm.BirimId.Value <= 0)
            ModelState.AddModelError("BirimId", "Taşınmaz birimi seçilmelidir.");
        if (!vm.KiraciId.HasValue || vm.KiraciId.Value <= 0)
            ModelState.AddModelError("KiraciId", "Kiracı seçilmelidir.");
        if (vm.StartDate == default)
            ModelState.AddModelError("StartDate", "Başlangıç tarihi zorunludur.");
        if (vm.EndDate == default)
            ModelState.AddModelError("EndDate", "Bitiş tarihi zorunludur.");
        if (vm.EndDate <= vm.StartDate)
            ModelState.AddModelError("EndDate", "Bitiş tarihi başlangıçtan sonra olmalıdır.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var (basarili, hata, _) = await _service.CreateAsync(vm, userId);

        if (!basarili)
        {
            ModelState.AddModelError(string.Empty, hata!);
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Reservation başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Reservation.Cancel)]
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
            TempData["Success"] = "Reservation iptal edildi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Reservation.TransferToCharge)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TahakkukaAktar(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var (basarili, hata, _) = await _service.TransferToChargeAsync(id, userId);

        if (!basarili)
            TempData["Error"] = hata;
        else
            TempData["Success"] = "Reservation tahakkuka aktarıldı.";

        return RedirectToAction(nameof(Index));
    }

    // AJAX: ücret önizleme
    [HttpGet]
    public async Task<IActionResult> Hesapla(int birimId, string baslangic, string bitis)
    {
        if (!DateTime.TryParse(baslangic, out var bas) || !DateTime.TryParse(bitis, out var bit))
            return BadRequest("Geçersiz tarih formatı.");

        var sonuc = await _service.HesaplaAsync(birimId, bas, bit);
        return Json(sonuc);
    }

    private async Task PopulateDropdownsAsync(RezervasyonCreateViewModel vm)
    {
        vm.RezervasyonBirimleri = await _birimRepo.GetRezervasyonBirimleriAsync();
        vm.Tenants = await _kiraciRepo.GetListAsync(null);
    }
}
