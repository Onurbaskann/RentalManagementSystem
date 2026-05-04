using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Authorization;
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

    public TasinmazController(
        ITasinmazService tasinmazService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager)
    {
        _tasinmazService = tasinmazService;
        _istatistik = istatistik;
        _userManager = userManager;
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

        var vm = new TasinmazDetayViewModel
        {
            Tasinmaz = t,
            Birimler = t.Birimler.Select(b => new BirimDetayViewModel
            {
                Birim = b,
                Durum = _istatistik.GetBirimDurumu(b),
                AktifSozlesme = _istatistik.GetAktifSozlesme(b)
            }).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public IActionResult Ekle()
    {
        return View(new TasinmazEkleViewModel());
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle(TasinmazEkleViewModel vm)
    {
        if (vm.Tipi != TasinmazTipi.Bina)
        {
            vm.KiralamaSekli = KiralamaSekli.TekParca;
            vm.KatSayisi = null;
            vm.Ofisler.Clear();
        }

        if (vm.Tipi == TasinmazTipi.Bina && vm.KiralamaSekli == KiralamaSekli.OfisBazli)
        {
            var gecerliOfisler = vm.Ofisler.Where(o => !string.IsNullOrWhiteSpace(o.OfisNo)).ToList();
            if (gecerliOfisler.Count == 0)
                ModelState.AddModelError("Ofisler", "En az bir ofis tanımlanmalıdır.");

            var tekrarlayanOfisNo = gecerliOfisler
                .GroupBy(o => o.OfisNo.Trim().ToUpper())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();
            if (tekrarlayanOfisNo != null)
                ModelState.AddModelError("Ofisler", $"Ofis No '{tekrarlayanOfisNo}' aynı bina içinde tekrar kullanılamaz.");

            var sifirM2 = gecerliOfisler.FirstOrDefault(o => o.Yuzolcumu <= 0);
            if (sifirM2 != null)
                ModelState.AddModelError("Ofisler", $"Ofis No '{sifirM2.OfisNo}' için yüzölçümü 0'dan büyük olmalıdır.");

            vm.Ofisler = gecerliOfisler;
        }
        else
        {
            vm.Ofisler.Clear();
        }

        if (!ModelState.IsValid) return View(vm);

        var t = new Tasinmaz
        {
            Ad = vm.Ad,
            Tipi = vm.Tipi,
            KiralamaSekli = vm.KiralamaSekli,
            Il = vm.Il,
            Ilce = vm.Ilce,
            Mahalle = vm.Mahalle,
            AcikAdres = vm.AcikAdres,
            AcikYuzolcumu = vm.AcikYuzolcumu,
            KapaliYuzolcumu = vm.KapaliYuzolcumu,
            KatSayisi = vm.Tipi == TasinmazTipi.Bina ? vm.KatSayisi : null,
            Aciklama = vm.Aciklama
        };

        await _tasinmazService.CreateAsync(t, vm.Ofisler.Count > 0 ? vm.Ofisler : null);
        TempData["Success"] = $"'{t.Ad}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}
