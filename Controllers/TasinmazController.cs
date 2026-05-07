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
public class TasinmazController : Controller
{
    private readonly ITasinmazService _tasinmazService;
    private readonly IIstatistikService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;

    public TasinmazController(
        ITasinmazService tasinmazService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx)
    {
        _tasinmazService = tasinmazService;
        _istatistik = istatistik;
        _userManager = userManager;
        _ctx = ctx;
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.View)]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole("Goruntuleyici") ? userId : null;
        var tasinmazlar = await _tasinmazService.GetAllAsync(filterUserId);
        return View(tasinmazlar);
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.View)]
    public async Task<IActionResult> Detay(int id)
    {
        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var tasinmazlar = await _tasinmazService.GetAllAsync(userId);
            if (!tasinmazlar.Any(t => t.Id == id)) return Forbid();
        }

        var t = await _tasinmazService.GetByIdAsync(id);
        if (t == null) return NotFound();

        var carpanlar = await _ctx.TasinmazKategoriCarpanlari
            .Include(c => c.KiraciKategori)
            .Where(c => c.TasinmazId == id)
            .OrderBy(c => c.KiraciKategori.Ad)
            .ToListAsync();

        var vm = new TasinmazDetayViewModel
        {
            Tasinmaz = t,
            Birimler = t.Birimler.Select(b => new BirimDetayViewModel
            {
                Birim = b,
                Durum = _istatistik.GetBirimDurumu(b),
                AktifSozlesme = _istatistik.GetAktifSozlesme(b)
            }).ToList(),
            Carpanlar = carpanlar
        };
        return View(vm);
    }

    private async Task PopulateViewBagAsync()
    {
        var birimTurleri = await _ctx.BirimTurleri.Where(b => b.Aktif).OrderBy(b => b.Sira).ToListAsync();
        ViewBag.BirimTurleri = birimTurleri.Where(b => b.KiralanabilirMi).ToList();
        ViewBag.RezervasyonBirimTurleri = birimTurleri.Where(b => b.RezervasyonYapilabilirMi).ToList();
        ViewBag.TasinmazTipleri = await _ctx.TasinmazTipleri.Where(t => t.Aktif).OrderBy(t => t.Sira).ToListAsync();
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle()
    {
        await PopulateViewBagAsync();
        return View(new TasinmazEkleViewModel());
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle(TasinmazEkleViewModel vm)
    {
        if (vm.KiralamaSekli == KiralamaSekli.BirimBazli)
        {
            var gecerliOfisler = vm.Ofisler.Where(o => !string.IsNullOrWhiteSpace(o.OfisNo)).ToList();
            if (gecerliOfisler.Count == 0)
                ModelState.AddModelError("Ofisler", "En az bir birim tanımlanmalıdır.");

            var tekrarlayanOfisNo = gecerliOfisler
                .GroupBy(o => o.OfisNo.Trim().ToUpper())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();
            if (tekrarlayanOfisNo != null)
                ModelState.AddModelError("Ofisler", $"Birim No '{tekrarlayanOfisNo}' aynı taşınmaz içinde tekrar kullanılamaz.");

            var sifirM2 = gecerliOfisler.FirstOrDefault(o => o.Yuzolcumu <= 0);
            if (sifirM2 != null)
                ModelState.AddModelError("Ofisler", $"Birim No '{sifirM2.OfisNo}' için yüzölçümü 0'dan büyük olmalıdır.");

            vm.Ofisler = gecerliOfisler;
        }
        else
        {
            vm.Ofisler.Clear();
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();
            return View(vm);
        }

        var t = new Tasinmaz
        {
            Ad = vm.Ad,
            TasinmazTipiId = vm.TasinmazTipiId,
            KiralamaSekli = vm.KiralamaSekli,
            Il = vm.Il,
            Ilce = vm.Ilce,
            Mahalle = vm.Mahalle,
            AcikAdres = vm.AcikAdres,
            AcikYuzolcumu = vm.AcikYuzolcumu,
            KapaliYuzolcumu = vm.KapaliYuzolcumu,
            KatSayisi = vm.KatSayisi,
            Aciklama = vm.Aciklama
        };

        var rezervasyonAlanlari = vm.RezervasyonAlanlari
            .Where(r => !string.IsNullOrWhiteSpace(r.Ad))
            .ToList();

        await _tasinmazService.CreateAsync(t,
            vm.Ofisler.Count > 0 ? vm.Ofisler : null,
            rezervasyonAlanlari.Count > 0 ? rezervasyonAlanlari : null);
        TempData["Success"] = $"'{t.Ad}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}
