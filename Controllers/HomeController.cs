using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
    private readonly IYetkiKapsamiProvider _provider;

    public HomeController(
        ITasinmazService tasinmazService,
        ISozlesmeService sozlesmeService,
        IIstatistikService istatistik,
        ITahakkukService tahakkukService,
        IOdemeService odemeService,
        IBankaHareketiService bankaHareketiService,
        IRezervasyonService rezervasyonService,
        UserManager<ApplicationUser> userManager,
        IYetkiKapsamiProvider provider)
    {
        _tasinmazService = tasinmazService;
        _sozlesmeService = sozlesmeService;
        _istatistik = istatistik;
        _tahakkukService = tahakkukService;
        _odemeService = odemeService;
        _bankaHareketiService = bankaHareketiService;
        _rezervasyonService = rezervasyonService;
        _userManager = userManager;
        _provider = provider;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.UserType == UserType.Kiraci)
            return RedirectToAction("Index", "KiraciPanel");

        var now = DateTime.Now;
        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;

        var tasinmazlar = await _tasinmazService.GetAllAsync(tasinmazIds);
        var sozlesmeler = await _sozlesmeService.GetAllAsync(tasinmazIds: tasinmazIds);
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync(tasinmazIds);

        var aktifSozlesmeler = sozlesmeler.Where(s => s.Aktif).ToList();
        decimal aylikToplamGelir = 0m;
        foreach (var s in aktifSozlesmeler)
            aylikToplamGelir += s.AylikBedel;

        var vm = new DashboardViewModel
        {
            ToplamTasinmaz = tasinmazlar.Count,
            TipiDagilim = tasinmazlar.GroupBy(t => string.IsNullOrEmpty(t.TasinmazTipiAd) ? "Diğer" : t.TasinmazTipiAd).ToDictionary(g => g.Key, g => g.Count()),
            ToplamBirim = tasinmazlar.Sum(t => t.BirimSayisi),
            KiraliBirim = tasinmazlar.Sum(t => t.KiraliBirimSayisi),
            BosBirim = tasinmazlar.Sum(t => t.BosBirimSayisi),
            SuresiDolmakUzereBirim = tasinmazlar.Sum(t => t.SuresiDolmakUzereBirimSayisi),
            AktifSozlesme = aktifSozlesmeler.Count,
            AylikToplamGelir = aylikToplamGelir,
            YillikProj = aylikToplamGelir * 12,
        };

        vm.BuAyYenilenecek = sozlesmeler
            .Count(s => s.Aktif && s.BitisTarihi.Year == now.Year && s.BitisTarihi.Month == now.Month);

        vm.SuresiDolmakUzere = sozlesmeler
            .Where(s => s.Aktif && s.KalanGun <= 60)
            .OrderBy(s => s.BitisTarihi)
            .Take(5)
            .Select(s => new SuresiDolmakUzereSozlesme
            {
                SozlesmeId = s.Id,
                KiraciAdi = s.KiraciGosterimAdi,
                TasinmazAdi = s.TasinmazAd,
                BirimAdi = s.BirimAd,
                KalanGun = s.KalanGun,
                BitisTarihi = s.BitisTarihi
            }).ToList();

        vm.BosBirimler = bosBirimler
            .Take(5)
            .Select(b => new BosBirimOzet
            {
                BirimId = b.Id,
                TasinmazAdi = b.TasinmazAd,
                BirimAdi = b.Ad,
                Ilce = b.Ilce,
                Yuzolcumu = b.Yuzolcumu
            }).ToList();

        if (User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.Odeme.View))
        {
            vm.HasOdemeAccess = true;
            await _tahakkukService.GecikmeleriGuncelleAsync();
            var tahakkuklar = await _tahakkukService.GetListAsync(tasinmazIds: tasinmazIds);
            var buAyTahakkuklar = tahakkuklar.Where(t => t.DonemBaslangic.Year == now.Year && t.DonemBaslangic.Month == now.Month).ToList();

            vm.BuAyBeklenenTahsilat = buAyTahakkuklar.Sum(t => t.ToplamTutar);
            vm.BuAyTahsilEdilen = buAyTahakkuklar.Sum(t => t.OdenenTutar);
            vm.GecikmisTahakkukAdet = tahakkuklar.Count(t => t.Durum == TahakkukDurumu.Gecikti);
            vm.GecikmisTutarToplam = tahakkuklar.Where(t => t.Durum == TahakkukDurumu.Gecikti).Sum(t => t.ToplamTutar - t.OdenenTutar);

            var odemeler = await _odemeService.GetAllAsync(tasinmazIds: tasinmazIds);
            vm.OnayBekleyenOdemeAdet = odemeler.Count(o => o.Durum == OdemeDurumu.OnayBekliyor);

            var eslesmemisler = await _bankaHareketiService.GetAllAsync(BankaEslesmeDurumu.Eslestirilmedi);
            vm.EslesmemisHareketAdet = eslesmemisler.Count;

            vm.BuAyManuelBorcToplami = buAyTahakkuklar
                .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Manuel && t.Durum != TahakkukDurumu.IptalEdildi)
                .Sum(t => t.ToplamTutar);
            vm.BuAyRezervasyonGeliri = buAyTahakkuklar
                .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Rezervasyon && t.Durum != TahakkukDurumu.IptalEdildi)
                .Sum(t => t.ToplamTutar);

            var rezervasyonlar = await _rezervasyonService.GetAllAsync(tasinmazIds);
            vm.TahakkukaAktarilmamisRezervasyonAdet = rezervasyonlar
                .Count(r => r.Durum == RezervasyonDurumu.Planlandi && r.ToplamTutar > 0 && r.TahakkukId == null);
        }

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
