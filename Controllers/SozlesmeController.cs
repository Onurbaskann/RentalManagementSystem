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
public class SozlesmeController : Controller
{
    private readonly ISozlesmeService _sozlesmeService;
    private readonly ITasinmazService _tasinmazService;
    private readonly IKiraciService _kiraciService;
    private readonly IIstatistikService _istatistik;
    private readonly ITahakkukService _tahakkukService;
    private readonly ITahakkukUretimService _tahakkukUretim;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;

    public SozlesmeController(
        ISozlesmeService sozlesmeService,
        ITasinmazService tasinmazService,
        IKiraciService kiraciService,
        IIstatistikService istatistik,
        ITahakkukService tahakkukService,
        ITahakkukUretimService tahakkukUretim,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx)
    {
        _sozlesmeService = sozlesmeService;
        _tasinmazService = tasinmazService;
        _kiraciService = kiraciService;
        _istatistik = istatistik;
        _tahakkukService = tahakkukService;
        _tahakkukUretim = tahakkukUretim;
        _userManager = userManager;
        _ctx = ctx;
    }

    [Authorize(Policy = PermissionCatalog.Sozlesme.View)]
    public async Task<IActionResult> Index(string? filtre)
    {
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole("Goruntuleyici") ? userId : null;
        var sozlesmeler = await _sozlesmeService.GetAllAsync(filtre, filterUserId);

        var now = DateTime.Today;
        var borcluSayisi = await _ctx.KiraTahakkuklar
            .Where(t => t.DonemBaslangic <= now
                && t.Durum != TahakkukDurumu.TamOdendi
                && t.Durum != TahakkukDurumu.IptalEdildi)
            .Select(t => t.KiraSozlesmesi.KiraciId)
            .Distinct()
            .CountAsync();
        ViewBag.BorcluSayisi = borcluSayisi;

        ViewBag.Filtre = filtre ?? "tum";
        return View(sozlesmeler);
    }

    [Authorize(Policy = PermissionCatalog.Sozlesme.View)]
    public async Task<IActionResult> Detay(int id)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var tasinmazlar = await _tasinmazService.GetAllAsync(userId);
            if (!tasinmazlar.Any(t => t.Id == s.Birim.TasinmazId)) return Forbid();
        }

        var gecmis = await _sozlesmeService.GetByBirimIdAsync(s.BirimId);

        var vm = new SozlesmeDetayViewModel
        {
            Sozlesme = s,
            KalanGun = _istatistik.KalanGun(s),
            AylikBedel = _istatistik.AylikBedel(s),
            YillikBedel = _istatistik.YillikBedel(s),
            Aktif = _istatistik.Aktif(s),
            SureYuzdesi = _istatistik.SureYuzdesi(s),
            Durum = _istatistik.GetBirimDurumu(s.Birim),
            GecmisSozlesmeler = gecmis.Where(x => x.Id != id).ToList()
        };

        if (User.HasClaim("permission", "Odeme.View"))
        {
            vm.HasOdemeAccess = true;
            vm.Tahakkuklar = await _tahakkukService.GetAllAsync(sozlesmeId: id);
        }

        var guncelTahakkuk = await _ctx.KiraTahakkuklar
            .Include(t => t.Kalemler).ThenInclude(k => k.BorcTipi)
            .Where(t => t.KiraSozlesmesiId == id && t.Durum != TahakkukDurumu.IptalEdildi)
            .OrderByDescending(t => t.DonemBaslangic)
            .FirstOrDefaultAsync();
        vm.GuncelKalemler = guncelTahakkuk?.Kalemler
            .Where(k => !k.BorcTipi.TekSeferlikMi)
            .OrderBy(k => k.BorcTipi.Sira).ToList() ?? new();
        vm.GuncelKalemDonemi = guncelTahakkuk?.DonemBaslangic;

        if (User.HasClaim("permission", PermissionCatalog.Sozlesme.OverrideRate) || User.IsInRole("Admin"))
        {
            vm.HasRateAccess = true;
            var aktifBorcTipleri = await _ctx.BorcTipleri
                .Where(b => b.Aktif).OrderBy(b => b.Sira).ToListAsync();
            var mevcutRateler = await _ctx.SozlesmeRateler
                .Where(r => r.SozlesmeId == id).ToListAsync();
            vm.PazarlikFiyatlari = aktifBorcTipleri.Select(bt =>
            {
                var rate = mevcutRateler.FirstOrDefault(r => r.BorcTipiId == bt.Id);
                return new SozlesmeRateSatiri
                {
                    RateId           = rate?.Id ?? 0,
                    BorcTipiId       = bt.Id,
                    BorcTipiAd       = bt.Ad,
                    BorcTipiKod      = bt.Kod,
                    OzelFiyatAktif   = rate != null,
                    HesaplamaYontemi = rate?.HesaplamaYontemi ?? HesaplamaYontemi.Sabit,
                    BirimDeger       = rate?.BirimDeger ?? 0,
                    KdvOrani         = rate?.KdvOrani ?? 0
                };
            }).ToList();
        }

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Create)]
    public async Task<IActionResult> Ekle(int? birimId)
    {
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync();
        var kiraciler = await _kiraciService.GetAllAsync();
        var vm = new SozlesmeEkleViewModel
        {
            BirimId = birimId,
            MevcutBirimler = bosBirimler,
            Kiraciler = kiraciler
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Create)]
    public async Task<IActionResult> Ekle(SozlesmeEkleViewModel vm)
    {
        vm.MevcutBirimler = await _tasinmazService.GetBosBirimlerAsync();
        vm.Kiraciler = await _kiraciService.GetAllAsync();

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

        await _sozlesmeService.CreateAsync(s);
        await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
        TempData["Success"] = "Sözleşme başarıyla oluşturuldu.";
        return RedirectToAction("Detay", new { id = s.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Extend)]
    public async Task<IActionResult> Uzat(int id, SozlesmeUzatViewModel vm)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
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

        await _sozlesmeService.UzatAsync(id, vm.YeniBitisTarihi, vm.YeniKiraBedeli,
            vm.KdvUygulanacakMi, vm.KdvOrani ?? 20, vm.TufeOrani, vm.Aciklama);
        await _tahakkukUretim.UretSozlesmeIcinAsync(id);

        TempData["Success"] = "Sözleşme süresi başarıyla uzatıldı.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Terminate)]
    public async Task<IActionResult> Feshet(int id, SozlesmeFesihViewModel vm)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
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

        await _sozlesmeService.FeshetAsync(id, vm.FesihTarihi, vm.FesihNedeni, vm.Aciklama);
        await _tahakkukUretim.IptalEtFutureTahakkuklarAsync(id, vm.FesihTarihi);
        TempData["Success"] = "Sözleşme başarıyla feshedildi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.OverrideRate)]
    public async Task<IActionResult> PazarlikFiyatGuncelle(int id, List<SozlesmeRateSatiri> pazarlikFiyatlari)
    {
        var mevcutRateler = await _ctx.SozlesmeRateler
            .Where(r => r.SozlesmeId == id).ToListAsync();

        foreach (var satir in pazarlikFiyatlari)
        {
            var mevcut = mevcutRateler.FirstOrDefault(r => r.BorcTipiId == satir.BorcTipiId);
            if (satir.OzelFiyatAktif)
            {
                if (mevcut == null)
                    _ctx.SozlesmeRateler.Add(new SozlesmeRate
                    {
                        SozlesmeId       = id,
                        BorcTipiId       = satir.BorcTipiId,
                        HesaplamaYontemi = satir.HesaplamaYontemi,
                        BirimDeger       = satir.BirimDeger,
                        KdvOrani         = satir.KdvOrani
                    });
                else
                {
                    mevcut.HesaplamaYontemi = satir.HesaplamaYontemi;
                    mevcut.BirimDeger       = satir.BirimDeger;
                    mevcut.KdvOrani         = satir.KdvOrani;
                }
            }
            else if (mevcut != null)
                _ctx.SozlesmeRateler.Remove(mevcut);
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Pazarlık fiyatları güncellendi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tahakkuk.Regenerate)]
    public async Task<IActionResult> YenidenUret(int id, DateTime baslangicTarihi)
    {
        var s = await _ctx.Sozlesmeler
            .Include(x => x.IslemGecmisi)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        await _tahakkukUretim.YenidenUretAsync(id, baslangicTarihi);

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            KiraSozlesmesiId = id,
            IslemTipi = SozlesmeIslemTipi.TahakkukYenidenUretim,
            IslemTarihi = DateTime.Now,
            Aciklama = $"{baslangicTarihi:MMMM yyyy} tarihinden itibaren ödenmemiş tahakkuklar yeniden üretildi."
        });

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{baslangicTarihi:MMMM yyyy} tarihinden itibaren ödenmemiş tahakkuklar yeniden üretildi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Edit)]
    public IActionResult HesaplaTufeKdv(decimal mevcutBedel, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani)
    {
        var sonuc = _istatistik.HesaplaKiraArtisi(mevcutBedel, tufeOrani, kdvUygulanacakMi, kdvOrani);
        return Json(sonuc);
    }
}
