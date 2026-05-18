using KiraTakip.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Dtos;

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
    private readonly ITarifeHiyerarsiService _tarifeHiyerarsisi;
    private readonly IMailService _mail;
    private readonly IPaymentLinkService _paymentLink;
    private readonly IRazorViewToStringRenderer _razorRenderer;
    private readonly IOptions<PaymentLinkSettings> _paymentLinkOptions;
    private readonly ILogger<SozlesmeController> _logger;

    public SozlesmeController(
        ISozlesmeService sozlesmeService,
        ITasinmazService tasinmazService,
        IKiraciService kiraciService,
        IIstatistikService istatistik,
        ITahakkukService tahakkukService,
        ITahakkukUretimService tahakkukUretim,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx,
        ITarifeHiyerarsiService tarifeHiyerarsisi,
        IMailService mail,
        IPaymentLinkService paymentLink,
        IRazorViewToStringRenderer razorRenderer,
        IOptions<PaymentLinkSettings> paymentLinkOptions,
        ILogger<SozlesmeController> logger)
    {
        _sozlesmeService = sozlesmeService;
        _tasinmazService = tasinmazService;
        _kiraciService = kiraciService;
        _istatistik = istatistik;
        _tahakkukService = tahakkukService;
        _tahakkukUretim = tahakkukUretim;
        _userManager = userManager;
        _ctx = ctx;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _mail = mail;
        _paymentLink = paymentLink;
        _razorRenderer = razorRenderer;
        _paymentLinkOptions = paymentLinkOptions;
        _logger = logger;
    }

    [Authorize(Policy = PermissionCatalog.Sozlesme.View)]
    public async Task<IActionResult> Index(string? filtre)
    {
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole(RoleNames.Goruntuleyici) ? userId : null;
        var sozlesmeler = await _sozlesmeService.GetAllAsync(filtre, filterUserId);

        var now = DateTime.Today;
        var esik = now.AddDays(_paymentLinkOptions.Value.ReminderDaysBefore);
        var borcluSayisi = await _ctx.KiraTahakkuklar
            .Where(t => t.VadeTarihi <= esik
                && t.Durum != TahakkukDurumu.TamOdendi
                && t.Durum != TahakkukDurumu.IptalEdildi
                && t.KiraSozlesmesi != null
                && t.KiraSozlesmesi.KiraciId != 0)
            .GroupBy(t => t.KiraSozlesmesi!.KiraciId)
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

        if (User.IsInRole(RoleNames.Goruntuleyici))
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
            AylikBedel = await _istatistik.AylikBedelAsync(s),
            YillikBedel = await _istatistik.YillikBedelAsync(s),
            Aktif = _istatistik.Aktif(s),
            SureYuzdesi = _istatistik.SureYuzdesi(s),
            Durum = _istatistik.GetBirimDurumu(s.Birim),
            GecmisSozlesmeler = gecmis.Where(x => x.Id != id).ToList()
        };

        if (User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.Odeme.View))
        {
            vm.HasOdemeAccess = true;
            await _tahakkukService.GecikmeleriGuncelleAsync();
            vm.Tahakkuklar = await _tahakkukService.GetAllAsync(sozlesmeId: id);
        }

        var bugun = DateTime.Today;
        var guncelTahakkuk = await _ctx.KiraTahakkuklar
            .Include(t => t.Kalemler).ThenInclude(k => k.BorcTipi)
            .Where(t => t.KiraSozlesmesiId == id && t.Durum != TahakkukDurumu.IptalEdildi && t.DonemBaslangic <= bugun)
            .OrderByDescending(t => t.DonemBaslangic)
            .FirstOrDefaultAsync();
        vm.GuncelKalemler = guncelTahakkuk?.Kalemler
            .Where(k => k.BorcTipi.Davranis == BorcTipiDavranisi.AylikSabit)
            .OrderBy(k => k.BorcTipi.Sira).ToList() ?? new();
        vm.GuncelKalemDonemi = guncelTahakkuk?.DonemBaslangic;

        vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(
            TarifeHiyerarsiKatmani.Sozlesme,
            tasinmazId: s.Birim.TasinmazId,
            birimId:    s.BirimId,
            kategoriId: s.Kiraci.KiraciKategoriId,
            yil:        s.BaslangicTarihi.Year);

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Create)]
    public async Task<IActionResult> Ekle(int? birimId)
    {
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync();
        var kiraciler = await _kiraciService.GetAllAsync();
        ViewBag.BirimYuzolcumular = System.Text.Json.JsonSerializer.Serialize(
            bosBirimler.ToDictionary(b => b.Id, b => (double)b.Yuzolcumu));
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

        var kiraKalemi = vm.SozlesmeKalemleri
            .FirstOrDefault(k => k.Davranis == BorcTipiDavranisi.AylikSabit);
        var depozitoKalemi = vm.SozlesmeKalemleri
            .FirstOrDefault(k => k.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik);

        var kdvUygulanacakMi = kiraKalemi != null && kiraKalemi.KdvOrani > 0;
        var kdvOrani = kdvUygulanacakMi ? kiraKalemi!.KdvOrani : 0;
        var kiraBedeli = vm.SozlesmeKalemleri
            .Where(k => k.Davranis == BorcTipiDavranisi.AylikSabit)
            .Sum(k => k.Tutar);
        var depozito = depozitoKalemi?.Tutar ?? 0;

        var s = new KiraSozlesmesi
        {
            BirimId = vm.BirimId!.Value,
            KiraciId = vm.KiraciId,
            BaslangicTarihi = vm.BaslangicTarihi,
            BitisTarihi = vm.BitisTarihi,
            Depozito = depozito,
            Notlar = vm.Notlar,
            Durum = SozlesmeDurumu.Aktif,
            KdvUygulanacakMi = kdvUygulanacakMi,
            KdvOrani = kdvOrani
        };

        await _sozlesmeService.CreateAsync(s, kiraBedeli);

        // Override kalemlerini kaydet
        if (vm.SozlesmeKalemleri != null && vm.SozlesmeKalemleri.Any())
        {
            foreach (var k in vm.SozlesmeKalemleri.Where(x => x.KullaniciDegistirdiMi))
            {
                var rate = new SozlesmeRate
                {
                    SozlesmeId       = s.Id,
                    BorcTipiId       = k.BorcTipiId,
                    BirimDeger       = k.BirimDeger,
                    HesaplamaYontemi = k.HesaplamaYontemi,
                    KdvOrani         = k.KdvOrani
                };
                _ctx.SozlesmeRateler.Add(rate);
            }
            await _ctx.SaveChangesAsync();
        }

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

        if (vm.TufeUygulanacakMi && vm.TufeOrani.HasValue && vm.TufeOrani.Value < 0)
            ModelState.AddModelError("TufeOrani", "TÜFE oranı negatif olamaz.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Detay", new { id });
        }

        var eskiBedel = await _istatistik.AylikBedelAsync(s);

        if (vm.TarifeyiGuncelle && vm.SozlesmeKalemleri != null && vm.SozlesmeKalemleri.Any())
        {
            var eskiRateler = await _ctx.SozlesmeRateler.Where(r => r.SozlesmeId == id).ToListAsync();
            _ctx.SozlesmeRateler.RemoveRange(eskiRateler);
            foreach (var k in vm.SozlesmeKalemleri.Where(x => x.KullaniciDegistirdiMi))
            {
                _ctx.SozlesmeRateler.Add(new SozlesmeRate
                {
                    SozlesmeId       = id,
                    BorcTipiId       = k.BorcTipiId,
                    BirimDeger       = k.BirimDeger,
                    HesaplamaYontemi = k.HesaplamaYontemi,
                    KdvOrani         = k.KdvOrani
                });
            }
            await _ctx.SaveChangesAsync();
        }

        var yeniRateler = await _ctx.SozlesmeRateler
            .Include(r => r.BorcTipi)
            .Where(r => r.SozlesmeId == id).ToListAsync();
        var yeniBedel = HesaplaAylikBedelHelper(yeniRateler, s.Birim.Yuzolcumu);

        await _sozlesmeService.UzatAsync(id, vm.YeniBitisTarihi, eskiBedel, yeniBedel,
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
    [Authorize(Policy = PermissionCatalog.Tahakkuk.Regenerate)]
    public async Task<IActionResult> YenidenUret(int id, DateTime baslangicTarihi,
        bool tarifeyiGuncelle = false, List<SozlesmeKalemInputDto>? sozlesmeKalemleri = null)
    {
        var s = await _ctx.Sozlesmeler
            .Include(x => x.IslemGecmisi)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        if (tarifeyiGuncelle && sozlesmeKalemleri != null && sozlesmeKalemleri.Any())
        {
            var eskiRateler = await _ctx.SozlesmeRateler.Where(r => r.SozlesmeId == id).ToListAsync();
            _ctx.SozlesmeRateler.RemoveRange(eskiRateler);
            foreach (var k in sozlesmeKalemleri.Where(x => x.KullaniciDegistirdiMi))
            {
                _ctx.SozlesmeRateler.Add(new SozlesmeRate
                {
                    SozlesmeId       = id,
                    BorcTipiId       = k.BorcTipiId,
                    BirimDeger       = k.BirimDeger,
                    HesaplamaYontemi = k.HesaplamaYontemi,
                    KdvOrani         = k.KdvOrani
                });
            }
            await _ctx.SaveChangesAsync();
        }

        await _tahakkukUretim.YenidenUretAsync(id, baslangicTarihi);

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            KiraSozlesmesiId = id,
            IslemTipi = SozlesmeIslemTipi.TahakkukYenidenUretim,
            IslemTarihi = DateTime.Now,
            Aciklama = $"{baslangicTarihi:MMMM yyyy} tarihinden itibaren ödenmemiş tahakkuklar yeniden üretildi."
                       + (tarifeyiGuncelle ? " (Tarife güncellendi.)" : "")
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

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetVarsayilanKalemler(int birimId, int kiraciId, DateTime baslangic, int? sozlesmeId = null)
    {
        var previews = await _tahakkukUretim.ComposeKalemlerAsync(birimId, kiraciId, baslangic, sozlesmeId);
        var result = previews.Select(p => new SozlesmeKalemInputDto
        {
            BorcTipiId           = p.BorcTipiId,
            BorcTipiAd           = p.BorcTipiAd,
            BorcTipiKod          = p.BorcTipiKod,
            Davranis             = p.Davranis,
            VarsayilanTutar      = p.Tutar,
            Tutar                = p.Tutar,
            BirimDeger           = p.BirimDeger,
            VarsayilanBirimDeger = p.BirimDeger,
            KdvOrani             = p.KdvOrani,
            HesaplamaYontemi     = p.HesaplamaYontemi,
            KaynakTipi           = p.KaynakTipi.ToString(),
            RateBulundu          = p.RateBulundu,
            KullaniciDegistirdiMi = false
        }).ToList();

        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Sozlesme.View)]
    public async Task<IActionResult> BorclularaMailGonder([FromServices] IBorcHatirlatmaService borcHatirlatmaService)
    {
        try
        {
            var sonuc = await borcHatirlatmaService.GonderAsync();
            var mesajParcalari = new List<string>();
            if (sonuc.BasariliGonderim > 0) mesajParcalari.Add($"{sonuc.BasariliGonderim} kiracıya e-posta gönderildi");
            if (sonuc.CooldownAtlanan > 0) mesajParcalari.Add($"{sonuc.CooldownAtlanan} kiracı (bekleme süresinde olduğu için) atlandı");
            if (sonuc.BasarisizGonderim > 0) mesajParcalari.Add($"{sonuc.BasarisizGonderim} gönderimde hata oluştu");
            if (mesajParcalari.Count == 0) mesajParcalari.Add("Gönderilecek tahakkuk bulunamadı");

            if (sonuc.BasarisizGonderim > 0)
                TempData["Error"] = string.Join(", ", mesajParcalari) + ". Detaylar için logları inceleyin.";
            else
                TempData["Success"] = string.Join(", ", mesajParcalari) + ".";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu hatırlatma işlemi sırasında beklenmeyen hata.");
            TempData["Error"] = "Beklenmeyen bir hata oluştu. Detaylar için logları inceleyin.";
        }

        return RedirectToAction("Index");
    }

    private static decimal HesaplaAylikBedelHelper(IEnumerable<SozlesmeRate> rates, decimal yuzolcumu) =>
        rates.Where(r => r.BorcTipi?.Davranis == BorcTipiDavranisi.AylikSabit)
             .Sum(r => r.HesaplamaYontemi == HesaplamaYontemi.M2 ? r.BirimDeger * yuzolcumu : r.BirimDeger);
}
