using System.Diagnostics;
using KiraTakip.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ITasinmazService _tasinmazService;
    private readonly ISozlesmeService _sozlesmeService;
    private readonly IIstatistikService _istatistik;
    private readonly ITahakkukService _tahakkukService;
    private readonly IOdemeService _odemeService;
    private readonly IBankaHareketiService _bankaHareketiService;
    private readonly IRezervasyonService _rezervasyonService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ITasinmazService tasinmazService,
        ISozlesmeService sozlesmeService,
        IIstatistikService istatistik,
        ITahakkukService tahakkukService,
        IOdemeService odemeService,
        IBankaHareketiService bankaHareketiService,
        IRezervasyonService rezervasyonService,
        UserManager<ApplicationUser> userManager)
    {
        _tasinmazService = tasinmazService;
        _sozlesmeService = sozlesmeService;
        _istatistik = istatistik;
        _tahakkukService = tahakkukService;
        _odemeService = odemeService;
        _bankaHareketiService = bankaHareketiService;
        _rezervasyonService = rezervasyonService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole(RoleNames.Goruntuleyici) ? userId : null;

        var tasinmazlar = await _tasinmazService.GetAllAsync(filterUserId);
        var tumBirimler = tasinmazlar.SelectMany(t => t.Birimler).ToList();
        var sozlesmeler = await _sozlesmeService.GetAllAsync(userId: filterUserId);
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync(filterUserId);

        var aktifSozlesmeler = sozlesmeler.Where(_istatistik.Aktif).ToList();
        decimal aylikToplamGelir = 0m;
        foreach (var s in aktifSozlesmeler)
            aylikToplamGelir += await _istatistik.AylikBedelAsync(s);

        var vm = new DashboardViewModel
        {
            ToplamTasinmaz = tasinmazlar.Count,
            TipiDagilim = tasinmazlar.GroupBy(t => t.TasinmazTipi?.Ad ?? "Diğer").ToDictionary(g => g.Key, g => g.Count()),
            ToplamBirim = tumBirimler.Count,
            AktifSozlesme = aktifSozlesmeler.Count,
            AylikToplamGelir = aylikToplamGelir,
            YillikProj = aylikToplamGelir * 12,
        };

        foreach (var birim in tumBirimler)
        {
            var durum = _istatistik.GetBirimDurumu(birim);
            if (durum == KiraDurumu.Kirali) vm.KiraliBirim++;
            else if (durum == KiraDurumu.SuresiDolmakUzere) vm.SuresiDolmakUzereBirim++;
            else vm.BosBirim++;
        }

        vm.BuAyYenilenecek = sozlesmeler
            .Count(s => _istatistik.Aktif(s) && s.BitisTarihi.Year == now.Year && s.BitisTarihi.Month == now.Month);

        vm.SuresiDolmakUzere = sozlesmeler
            .Where(s => _istatistik.Aktif(s) && _istatistik.KalanGun(s) <= 60)
            .OrderBy(s => s.BitisTarihi)
            .Take(5)
            .Select(s => new SuresiDolmakUzereSozlesme
            {
                SozlesmeId = s.Id,
                KiraciAdi = s.Kiraci.GosterimAdi,
                TasinmazAdi = s.Birim.Tasinmaz.Ad,
                BirimAdi = s.Birim.Ad,
                KalanGun = _istatistik.KalanGun(s),
                BitisTarihi = s.BitisTarihi
            }).ToList();

        vm.BosBirimler = bosBirimler
            .Take(5)
            .Select(b => new BosBirimOzet
            {
                BirimId = b.Id,
                TasinmazAdi = b.Tasinmaz.Ad,
                BirimAdi = b.Ad,
                Ilce = b.Tasinmaz.Ilce,
                Yuzolcumu = b.Yuzolcumu
            }).ToList();

        if (User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.Odeme.View))
        {
            vm.HasOdemeAccess = true;
            var tahakkuklar = await _tahakkukService.GetAllAsync(userId: filterUserId);
            var buAyTahakkuklar = tahakkuklar.Where(t => t.DonemBaslangic.Year == now.Year && t.DonemBaslangic.Month == now.Month).ToList();

            vm.BuAyBeklenenTahsilat = buAyTahakkuklar.Sum(t => t.ToplamTutar);
            vm.BuAyTahsilEdilen     = buAyTahakkuklar.Sum(t => t.OdenenTutar);
            vm.GecikmisTahakkukAdet = tahakkuklar.Count(t => t.Durum == TahakkukDurumu.Gecikti);
            vm.GecikmisTutarToplam  = tahakkuklar.Where(t => t.Durum == TahakkukDurumu.Gecikti).Sum(t => t.ToplamTutar - t.OdenenTutar);

            var odemeler = await _odemeService.GetAllAsync(userId: filterUserId);
            vm.OnayBekleyenOdemeAdet = odemeler.Count(o => o.Durum == OdemeDurumu.OnayBekliyor);

            var eslesmemisler = await _bankaHareketiService.GetAllAsync(BankaEslesmeDurumu.Eslestirilmedi);
            vm.EslesmemisHareketAdet = eslesmemisler.Count;

            vm.BuAyManuelBorcToplami = buAyTahakkuklar
                .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Manuel && t.Durum != TahakkukDurumu.IptalEdildi)
                .Sum(t => t.ToplamTutar);
            vm.BuAyRezervasyonGeliri = buAyTahakkuklar
                .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Rezervasyon && t.Durum != TahakkukDurumu.IptalEdildi)
                .Sum(t => t.ToplamTutar);

            var rezervasyonlar = await _rezervasyonService.GetAllAsync(userId: filterUserId);
            vm.TahakkukaAktarilmamisRezervasyonAdet = rezervasyonlar
                .Count(r => r.Durum == RezervasyonDurumu.Planlandi && r.ToplamTutar > 0 && r.KiraTahakkukId == null);
        }

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
