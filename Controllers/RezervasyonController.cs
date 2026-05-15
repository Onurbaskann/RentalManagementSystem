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
public class RezervasyonController : Controller
{
    private readonly IRezervasyonService _service;
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public RezervasyonController(IRezervasyonService service, ApplicationDbContext ctx,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _ctx = ctx;
        _userManager = userManager;
    }

    [Authorize(Policy = PermissionCatalog.Rezervasyon.View)]
    public async Task<IActionResult> Index()
    {
        var userId = User.IsInRole("Goruntuleyici") ? _userManager.GetUserId(User) : null;
        var liste = await _service.GetAllAsync(userId);
        return View(liste);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Rezervasyon.Create)]
    public async Task<IActionResult> Ekle()
    {
        var vm = new RezervasyonCreateViewModel
        {
            BaslangicTarihi = DateTime.Today.AddHours(9),
            BitisTarihi = DateTime.Today.AddHours(11)
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Rezervasyon.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(RezervasyonCreateViewModel vm)
    {
        if (vm.BirimId <= 0)
            ModelState.AddModelError("BirimId", "Birim seçilmelidir.");
        if (vm.KiraciId <= 0)
            ModelState.AddModelError("KiraciId", "Kiracı seçilmelidir.");
        if (vm.BaslangicTarihi == default)
            ModelState.AddModelError("BaslangicTarihi", "Başlangıç tarihi zorunludur.");
        if (vm.BitisTarihi == default)
            ModelState.AddModelError("BitisTarihi", "Bitiş tarihi zorunludur.");
        if (vm.BitisTarihi <= vm.BaslangicTarihi)
            ModelState.AddModelError("BitisTarihi", "Bitiş tarihi başlangıçtan sonra olmalıdır.");

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

        TempData["Success"] = "Rezervasyon başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Rezervasyon.Cancel)]
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
            TempData["Success"] = "Rezervasyon iptal edildi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Rezervasyon.TransferToTahakkuk)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TahakkukaAktar(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var (basarili, hata, _) = await _service.TransferToTahakkukAsync(id, userId);

        if (!basarili)
            TempData["Error"] = hata;
        else
            TempData["Success"] = "Rezervasyon tahakkuka aktarıldı.";

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
        vm.RezervasyonBirimleri = await _ctx.Birimler
            .Include(b => b.BirimTuru)
            .Include(b => b.Tasinmaz)
            .Where(b => b.BirimTuru != null && b.BirimTuru.RezervasyonYapilabilirMi && b.BirimTuru.Aktif)
            .OrderBy(b => b.Tasinmaz.Ad).ThenBy(b => b.Ad)
            .ToListAsync();

        vm.Kiraciler = await _ctx.Kiraciler
            .OrderBy(k => k.Ad).ThenBy(k => k.Soyad)
            .ToListAsync();

        vm.Sozlesmeler = await _ctx.Sozlesmeler
            .Include(s => s.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(s => s.Kiraci)
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .OrderBy(s => s.Kiraci.Ad)
            .ToListAsync();
    }
}
