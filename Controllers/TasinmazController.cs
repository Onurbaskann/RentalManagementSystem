using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services;

namespace KiraTakip.Controllers;

[Authorize]
public class TasinmazController : Controller
{
    private readonly DummyDataService _data;
    private readonly IstatistikService _istatistik;
    private readonly UserTasinmazYetkiService _yetkiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TasinmazController(
        DummyDataService data, 
        IstatistikService istatistik,
        UserTasinmazYetkiService yetkiService,
        UserManager<ApplicationUser> userManager)
    {
        _data = data;
        _istatistik = istatistik;
        _yetkiService = yetkiService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId!);
            var filtered = _data.Tasinmazlar.Where(t => yetkiliIds.Contains(t.Id)).ToList();
            return View(filtered);
        }
        return View(_data.Tasinmazlar);
    }

    public async Task<IActionResult> Detay(int id)
    {
        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var canView = await _yetkiService.CanViewTasinmazAsync(userId!, id);
            if (!canView) return Forbid();
        }

        var t = _data.GetTasinmaz(id);
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
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Ekle()
    {
        return View(new TasinmazEkleViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Ekle(TasinmazEkleViewModel vm)
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

        _data.TasinmazEkle(t, vm.Ofisler.Count > 0 ? vm.Ofisler : null);
        TempData["Success"] = $"'{t.Ad}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}
