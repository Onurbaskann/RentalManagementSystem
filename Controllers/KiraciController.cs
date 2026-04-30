using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services;

namespace KiraTakip.Controllers;

[Authorize]
public class KiraciController : Controller
{
    private readonly DummyDataService _data;
    private readonly IstatistikService _istatistik;
    private readonly UserTasinmazYetkiService _yetkiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public KiraciController(
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
            
            // Bu kullanıcının yetkili olduğu taşınmazlarda sözleşmesi olan kiracıları bul
            var kiraciIds = _data.Sozlesmeler
                .Where(s => yetkiliIds.Contains(s.Birim.TasinmazId))
                .Select(s => s.KiraciId)
                .Distinct();

            var filtered = _data.Kiraciler.Where(k => kiraciIds.Contains(k.Id)).ToList();
            return View(filtered);
        }
        return View(_data.Kiraciler);
    }

    public async Task<IActionResult> Detay(int id)
    {
        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId!);
            
            // Kiracının yetkili taşınmazlarda en az bir sözleşmesi var mı?
            var hasAccess = _data.Sozlesmeler
                .Any(s => s.KiraciId == id && yetkiliIds.Contains(s.Birim.TasinmazId));
            
            if (!hasAccess) return Forbid();
        }

        var k = _data.GetKiraci(id);
        if (k == null) return NotFound();

        var vm = new KiraciDetayViewModel
        {
            Kiraci = k,
            Sozlesmeler = _data.Sozlesmeler
                .Where(s => s.KiraciId == id)
                .OrderByDescending(s => s.BaslangicTarihi)
                .ToList()
        };
        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Ekle()
    {
        var vm = new KiraciFormViewModel
        {
            KiraciNo = _data.GenerateKiraciNo()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Ekle(KiraciFormViewModel vm)
    {
        // Otomatik validation hatalarını temizle (Ad ve Soyad için biz manuel yapıyoruz)
        ModelState.Remove("Ad");
        ModelState.Remove("GercekAd");
        ModelState.Remove("TuzelAd");
        ModelState.Remove("Soyad");

        // Tür bazlı Ad eşlemesi ve validasyonu
        if (vm.KiraciTuru == KiraciTuru.Gercek)
        {
            vm.Ad = vm.GercekAd;
            if (string.IsNullOrWhiteSpace(vm.GercekAd))
                ModelState.AddModelError("GercekAd", "Lütfen bir Ad giriniz.");
            
            if (string.IsNullOrWhiteSpace(vm.Soyad))
                ModelState.AddModelError("Soyad", "Lütfen bir Soyad giriniz.");
        }
        else if (vm.KiraciTuru == KiraciTuru.Tuzel)
        {
            vm.Ad = vm.TuzelAd;
            if (string.IsNullOrWhiteSpace(vm.TuzelAd))
                ModelState.AddModelError("TuzelAd", "Lütfen bir Firma / Kurum adı giriniz.");
        }

        if (_data.KiraciNoExists(vm.KiraciNo))
            ModelState.AddModelError("KiraciNo", "Bu Kiracı No zaten kullanımda.");

        if (!ModelState.IsValid) return View(vm);

        var k = BuildKiraciFromVm(vm);
        _data.AddKiraci(k);
        TempData["Success"] = $"'{k.GosterimAdi}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = k.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Duzenle(int id)
    {
        var k = _data.GetKiraci(id);
        if (k == null) return NotFound();

        var vm = new KiraciFormViewModel
        {
            Id = k.Id,
            KiraciNo = k.KiraciNo,
            KiraciTuru = k.KiraciTuru,
            Ad = k.Ad,
            GercekAd = k.KiraciTuru == KiraciTuru.Gercek ? k.Ad : null,
            TuzelAd = k.KiraciTuru == KiraciTuru.Tuzel ? k.Ad : null,
            Soyad = k.Soyad,
            TcKimlikNo = k.TcKimlikNo,
            PasaportNo = k.PasaportNo,
            Unvan = k.Unvan,
            AnneAdi = k.AnneAdi,
            BabaAdi = k.BabaAdi,
            DogumTarihi = k.DogumTarihi,
            DogumYeri = k.DogumYeri,
            TicaretSicilNo = k.TicaretSicilNo,
            VergiNo = k.VergiNo,
            VergiDairesi = k.VergiDairesi,
            MersisNo = k.MersisNo,
            Telefon = k.Telefon,
            Email = k.Email,
            Adres = k.Adres
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Duzenle(int id, KiraciFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        // Otomatik validation hatalarını temizle
        ModelState.Remove("Ad");
        ModelState.Remove("GercekAd");
        ModelState.Remove("TuzelAd");
        ModelState.Remove("Soyad");

        // Tür bazlı Ad eşlemesi ve validasyonu
        if (vm.KiraciTuru == KiraciTuru.Gercek)
        {
            vm.Ad = vm.GercekAd;
            if (string.IsNullOrWhiteSpace(vm.GercekAd))
                ModelState.AddModelError("GercekAd", "Lütfen bir Ad giriniz.");
            
            if (string.IsNullOrWhiteSpace(vm.Soyad))
                ModelState.AddModelError("Soyad", "Lütfen bir Soyad giriniz.");
        }
        else if (vm.KiraciTuru == KiraciTuru.Tuzel)
        {
            vm.Ad = vm.TuzelAd;
            if (string.IsNullOrWhiteSpace(vm.TuzelAd))
                ModelState.AddModelError("TuzelAd", "Lütfen bir Firma / Kurum adı giriniz.");
        }

        if (_data.KiraciNoExists(vm.KiraciNo, excludeId: id))
            ModelState.AddModelError("KiraciNo", "Bu Kiracı No zaten kullanımda.");

        if (!ModelState.IsValid) return View(vm);

        var k = BuildKiraciFromVm(vm);
        k.Id = id;
        _data.UpdateKiraci(k);
        TempData["Success"] = "Kiracı bilgileri güncellendi.";
        return RedirectToAction("Detay", new { id });
    }

    private static Kiraci BuildKiraciFromVm(KiraciFormViewModel vm)
    {
        var tur = vm.KiraciTuru ?? KiraciTuru.Gercek;
        var k = new Kiraci
        {
            KiraciNo = vm.KiraciNo,
            KiraciTuru = tur,
            Ad = vm.Ad,
            Telefon = vm.Telefon,
            Email = vm.Email,
            Adres = vm.Adres
        };

        if (tur == KiraciTuru.Gercek)
        {
            k.Soyad = vm.Soyad;
            k.TcKimlikNo = vm.TcKimlikNo;
            k.PasaportNo = vm.PasaportNo;
            k.Unvan = vm.Unvan;
            k.AnneAdi = vm.AnneAdi;
            k.BabaAdi = vm.BabaAdi;
            k.DogumTarihi = vm.DogumTarihi;
            k.DogumYeri = vm.DogumYeri;
            // Tüzel alanlar temizlenir
            k.TicaretSicilNo = null;
            k.VergiNo = null;
            k.VergiDairesi = null;
            k.MersisNo = null;
        }
        else
        {
            k.TicaretSicilNo = vm.TicaretSicilNo;
            k.VergiNo = vm.VergiNo;
            k.VergiDairesi = vm.VergiDairesi;
            k.MersisNo = vm.MersisNo;
            // Gerçek kişi alanları temizlenir
            k.Soyad = null;
            k.TcKimlikNo = null;
            k.PasaportNo = null;
            k.Unvan = null;
            k.AnneAdi = null;
            k.BabaAdi = null;
            k.DogumTarihi = null;
            k.DogumYeri = null;
        }

        return k;
    }
}
