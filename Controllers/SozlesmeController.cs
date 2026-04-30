using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services;

namespace KiraTakip.Controllers;

[Authorize]
public class SozlesmeController : Controller
{
    private readonly DummyDataService _data;
    private readonly IstatistikService _istatistik;
    private readonly UserTasinmazYetkiService _yetkiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SozlesmeController(
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

    public async Task<IActionResult> Index(string? filtre)
    {
        var sozlesmeler = _data.Sozlesmeler.AsEnumerable();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId!);
            sozlesmeler = sozlesmeler.Where(s => yetkiliIds.Contains(s.Birim.TasinmazId));
        }

        sozlesmeler = filtre switch
        {
            "aktif"      => sozlesmeler.Where(_istatistik.Aktif),
            "surek"      => sozlesmeler.Where(s => _istatistik.Aktif(s) && _istatistik.KalanGun(s) <= 30),
            "gecmis"     => sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.SonaErdi),
            "feshedildi" => sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.Feshedildi),
            _            => sozlesmeler
        };

        ViewBag.Filtre = filtre ?? "tum";
        return View(sozlesmeler.OrderByDescending(s => s.BaslangicTarihi).ToList());
    }

    public async Task<IActionResult> Detay(int id)
    {
        var s = _data.GetSozlesme(id);
        if (s == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var canView = await _yetkiService.CanViewTasinmazAsync(userId!, s.Birim.TasinmazId);
            if (!canView) return Forbid();
        }

        var gecmis = s.Birim.Sozlesmeler
            .Where(x => x.Id != id)
            .OrderByDescending(x => x.BaslangicTarihi)
            .ToList();

        var vm = new SozlesmeDetayViewModel
        {
            Sozlesme = s,
            KalanGun = _istatistik.KalanGun(s),
            AylikBedel = _istatistik.AylikBedel(s),
            YillikBedel = _istatistik.YillikBedel(s),
            Aktif = _istatistik.Aktif(s),
            SureYuzdesi = _istatistik.SureYuzdesi(s),
            Durum = _istatistik.GetBirimDurumu(s.Birim),
            GecmisSozlesmeler = gecmis
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Ekle(int? birimId)
    {
        var vm = new SozlesmeEkleViewModel
        {
            BirimId = birimId,
            MevcutBirimler = _data.GetBosBirimler(),
            Kiraciler = _data.Kiraciler
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Ekle(SozlesmeEkleViewModel vm)
    {
        vm.MevcutBirimler = _data.GetBosBirimler();
        vm.Kiraciler = _data.Kiraciler;

        if (vm.BirimId == null || vm.BirimId == 0)
            ModelState.AddModelError("BirimId", "Lütfen bir birim seçin.");

        if (vm.BitisTarihi <= vm.BaslangicTarihi)
            ModelState.AddModelError("BitisTarihi", "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.");

        if (!ModelState.IsValid) return View(vm);

        var s = new KiraSozlesmesi
        {
            BirimId = vm.BirimId!.Value,
            KiraciId = vm.KiraciId,
            BaslangicTarihi = vm.BaslangicTarihi,
            BitisTarihi = vm.BitisTarihi,
            KiraBedeli = vm.KiraBedeli,
            Periyot = vm.Periyot,
            Depozito = vm.Depozito,
            Notlar = vm.Notlar,
            Durum = SozlesmeDurumu.Aktif,
            KdvUygulanacakMi = vm.KdvUygulanacakMi,
            KdvOrani = vm.KdvUygulanacakMi ? vm.KdvOrani : 0
        };

        _data.SozlesmeEkle(s);
        TempData["Success"] = "Sözleşme başarıyla oluşturuldu.";
        return RedirectToAction("Detay", new { id = s.Id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Uzat(int id, SozlesmeUzatViewModel vm)
    {
        var s = _data.GetSozlesme(id);
        if (s == null) return NotFound();

        if (s.Durum == SozlesmeDurumu.Feshedildi)
        {
            TempData["Error"] = "Feshedilmiş sözleşme uzatılamaz.";
            return RedirectToAction("Detay", new { id });
        }

        if (vm.YeniBitisTarihi <= s.BitisTarihi)
            ModelState.AddModelError("YeniBitisTarihi", "Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.");

        if (vm.YeniKiraBedeli <= 0)
            ModelState.AddModelError("YeniKiraBedeli", "Yeni kira bedeli sıfırdan büyük olmalıdır.");

        if (vm.TufeUygulanacakMi && vm.TufeOrani.HasValue && vm.TufeOrani.Value < 0)
            ModelState.AddModelError("TufeOrani", "TÜFE oranı negatif olamaz.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Detay", new { id });
        }

        _data.UzatSozlesme(id, vm.YeniBitisTarihi, vm.YeniKiraBedeli,
            vm.KdvUygulanacakMi, vm.KdvOrani ?? 20, vm.TufeOrani, vm.Aciklama);

        TempData["Success"] = "Sözleşme süresi başarıyla uzatıldı.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult Feshet(int id, SozlesmeFesihViewModel vm)
    {
        var s = _data.GetSozlesme(id);
        if (s == null) return NotFound();

        if (s.Durum == SozlesmeDurumu.Feshedildi)
        {
            TempData["Error"] = "Sözleşme zaten feshedilmiş.";
            return RedirectToAction("Detay", new { id });
        }

        if (string.IsNullOrWhiteSpace(vm.FesihNedeni))
            ModelState.AddModelError("FesihNedeni", "Fesih nedeni zorunludur.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Detay", new { id });
        }

        _data.FeshetSozlesme(id, vm.FesihTarihi, vm.FesihNedeni, vm.Aciklama);
        TempData["Success"] = "Sözleşme başarıyla feshedildi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Yonetici")]
    public IActionResult HesaplaTufeKdv(decimal mevcutBedel, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani)
    {
        var sonuc = _istatistik.HesaplaKiraArtisi(mevcutBedel, tufeOrani, kdvUygulanacakMi, kdvOrani);
        return Json(sonuc);
    }
}
