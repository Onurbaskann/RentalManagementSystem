using System.Diagnostics;
using System.Globalization;
using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using KiraTakip.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        var today = DateTime.Today;
        var trCulture = CultureInfo.GetCultureInfo("tr-TR");
        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;

        var tasinmazlar = await _tasinmazService.GetAllAsync(tasinmazIds);
        var sozlesmeler = await _sozlesmeService.GetAllAsync(tasinmazIds: tasinmazIds);
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync(tasinmazIds);

        var aktifSozlesmeler = sozlesmeler.Where(s => s.Aktif).ToList();
        decimal aylikToplamGelir = 0m;
        foreach (var s in aktifSozlesmeler)
            aylikToplamGelir += s.AylikBedel;

        var roller = user?.IsSuperAdmin == true ? RoleNames.SistemYoneticisi
            : User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value 
            ?? "Kullanıcı";

        var vm = new DashboardViewModel
        {
            KullaniciAd = user?.AdSoyad ?? user?.Email ?? "Kullanıcı",
            KullaniciRol = roller,
            TarihEtiket = today.ToString("d MMMM yyyy, dddd", trCulture),
            ToplamTasinmaz = tasinmazlar.Count,
            TipiDagilim = tasinmazlar.GroupBy(t => string.IsNullOrEmpty(t.TasinmazTipiAd) ? "Diğer" : t.TasinmazTipiAd).ToDictionary(g => g.Key, g => g.Count()),
            ToplamBirim = tasinmazlar.Sum(t => t.BirimSayisi),
            KiraliBirim = tasinmazlar.Sum(t => t.KiraliBirimSayisi),
            BosBirim = tasinmazlar.Sum(t => t.BosBirimSayisi),
            SuresiDolmakUzereBirim = tasinmazlar.Sum(t => t.SuresiDolmakUzereBirimSayisi),
            AktifSozlesme = aktifSozlesmeler.Count,
            AktifKiraciSayisi = aktifSozlesmeler.Select(s => s.KiraciId).Distinct().Count(),
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

        if (User.HasModuleAccess("Internal.Odeme"))
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

            // --- Redesign metrikleri ---
            // Son 6 ay nakit akışı + tahsilat oranı sparkline
            var sonAltiAyBaslangic = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            var aylikGroup = tahakkuklar
                .Where(t => t.DonemBaslangic >= sonAltiAyBaslangic && t.Durum != TahakkukDurumu.IptalEdildi)
                .GroupBy(t => new { t.DonemBaslangic.Year, t.DonemBaslangic.Month })
                .ToDictionary(
                    g => (g.Key.Year, g.Key.Month),
                    g => (Beklenen: g.Sum(t => t.ToplamTutar), Odenen: g.Sum(t => t.OdenenTutar)));

            for (int i = 5; i >= 0; i--)
            {
                var ay = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var bucket = aylikGroup.TryGetValue((ay.Year, ay.Month), out var v) ? v : (Beklenen: 0m, Odenen: 0m);
                vm.AylikNakit.Add(new DashboardAylikNakit
                {
                    AyEtiket = trCulture.DateTimeFormat.GetAbbreviatedMonthName(ay.Month),
                    Beklenen = bucket.Beklenen,
                    Odenen = bucket.Odenen
                });
                var oran = bucket.Beklenen > 0 ? (double)(bucket.Odenen / bucket.Beklenen) * 100 : 0;
                vm.TahsilatOraniSparkline.Add(Math.Round(oran, 1));
            }

            // Tahsilat oranı — son 30 gün vade dolan tahakkuklar
            var otuzGunOnce = today.AddDays(-30);
            var son30 = tahakkuklar
                .Where(t => t.VadeTarihi >= otuzGunOnce && t.VadeTarihi <= today && t.Durum != TahakkukDurumu.IptalEdildi)
                .ToList();
            var bek30 = son30.Sum(t => t.ToplamTutar);
            var od30 = son30.Sum(t => t.OdenenTutar);
            vm.TahsilatOrani30Gun = bek30 > 0 ? Math.Round(od30 / bek30 * 100m, 1) : 0m;

            // Momentum — bu ay vs geçen ay (beklenen tahsilat üzerinden)
            var gecenAyStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var gecenAyEnd = gecenAyStart.AddMonths(1).AddDays(-1);
            vm.AylikGelirGecenAy = tahakkuklar
                .Where(t => t.DonemBaslangic >= gecenAyStart && t.DonemBaslangic <= gecenAyEnd && t.Durum != TahakkukDurumu.IptalEdildi)
                .Sum(t => t.ToplamTutar);
            vm.AylikGelirDelta = vm.AylikGelirGecenAy > 0
                ? Math.Round((vm.BuAyBeklenenTahsilat - vm.AylikGelirGecenAy) / vm.AylikGelirGecenAy * 100m, 1)
                : 0m;

            // Bugün vade dolan
            var bugun = tahakkuklar.Where(t => t.VadeTarihi.Date == today &&
                (t.Durum == TahakkukDurumu.Bekleniyor ||
                 t.Durum == TahakkukDurumu.KismenOdendi ||
                 t.Durum == TahakkukDurumu.Gecikti)).ToList();
            vm.BugunVadeDolanAdet = bugun.Count;
            vm.BugunVadeDolanTutar = bugun.Sum(t => t.ToplamTutar - t.OdenenTutar);

            // Top 5 gelir getiren taşınmaz (son 12 ay tahakkuk dönemleri, ödenen tutara göre)
            var sonYil = today.AddYears(-1);
            var birimSayisiByTasinmaz = tasinmazlar.ToDictionary(x => x.Id, x => x.BirimSayisi);
            vm.TopGelirTasinmaz = tahakkuklar
                .Where(t => t.DonemBaslangic >= sonYil && t.TasinmazId != null && t.OdenenTutar > 0)
                .GroupBy(t => new { TasinmazId = t.TasinmazId!.Value, TasinmazAd = t.TasinmazAd ?? "—" })
                .Select(g => new DashboardGelirTasinmaz
                {
                    TasinmazId = g.Key.TasinmazId,
                    TasinmazAd = g.Key.TasinmazAd,
                    ToplamTahsilat = g.Sum(t => t.OdenenTutar),
                    BirimSayisi = birimSayisiByTasinmaz.TryGetValue(g.Key.TasinmazId, out var bs) ? bs : 0
                })
                .OrderByDescending(x => x.ToplamTahsilat)
                .Take(5)
                .ToList();

            vm.TopGelirKiraci = tahakkuklar
                .Where(t => t.DonemBaslangic >= sonYil && t.OdenenTutar > 0)
                .GroupBy(t => new { t.KiraciId, KiraciAd = t.KiraciGosterimAdi ?? "—" })
                .Select(g => new DashboardGelirKiraci
                {
                    KiraciId = g.Key.KiraciId,
                    KiraciAd = g.Key.KiraciAd,
                    ToplamTahsilat = g.Sum(t => t.OdenenTutar),
                    SozlesmeSayisi = g.Select(t => t.KiraSozlesmesiId).Distinct().Count()
                })
                .OrderByDescending(x => x.ToplamTahsilat)
                .Take(5)
                .ToList();
        }

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
