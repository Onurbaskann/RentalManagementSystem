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
    private readonly IPropertyService _tasinmazService;
    private readonly ILeaseService _sozlesmeService;
    private readonly IStatisticsService _istatistik;
    private readonly IChargeService _chargeService;
    private readonly IPaymentService _paymentService;
    private readonly IBankTransactionService _bankTransactionService;
    private readonly IReservationService _reservationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionScopeProvider _provider;

    public HomeController(
        IPropertyService propertyService,
        ILeaseService leaseService,
        IStatisticsService istatistik,
        IChargeService chargeService,
        IPaymentService odemeService,
        IBankTransactionService bankaHareketiService,
        IReservationService rezervasyonService,
        UserManager<ApplicationUser> userManager,
        IPermissionScopeProvider provider)
    {
        _tasinmazService = propertyService;
        _sozlesmeService = leaseService;
        _istatistik = istatistik;
        _chargeService = chargeService;
        _paymentService = odemeService;
        _bankTransactionService = bankaHareketiService;
        _reservationService = rezervasyonService;
        _userManager = userManager;
        _provider = provider;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.UserType == UserType.Tenant)
            return RedirectToAction("Index", "TenantPanel");

        var now = DateTime.Now;
        var today = DateTime.Today;
        var trCulture = CultureInfo.GetCultureInfo("tr-TR");
        var propertyIds = _provider.GlobalAccess ? null : _provider.AccessiblePropertyIds;

        var tasinmazlar = await _tasinmazService.GetAllAsync(propertyIds);
        var sozlesmeler = await _sozlesmeService.GetAllAsync(propertyIds: propertyIds);
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync(propertyIds);

        var aktifSozlesmeler = sozlesmeler.Where(s => s.Aktif).ToList();
        decimal aylikToplamGelir = 0m;
        foreach (var s in aktifSozlesmeler)
            aylikToplamGelir += s.MonthlyAmount;

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
            AktifKiraciSayisi = aktifSozlesmeler.Select(s => s.TenantId).Distinct().Count(),
            AylikToplamGelir = aylikToplamGelir,
            YillikProj = aylikToplamGelir * 12,
        };

        vm.BuAyYenilenecek = sozlesmeler
            .Count(s => s.Aktif && s.EndDate.Year == now.Year && s.EndDate.Month == now.Month);

        vm.SuresiDolmakUzere = sozlesmeler
            .Where(s => s.Aktif && s.KalanGun <= 60)
            .OrderBy(s => s.EndDate)
            .Take(5)
            .Select(s => new SuresiDolmakUzereSozlesme
            {
                SozlesmeId = s.Id,
                KiraciAdi = s.TenantDisplayName,
                PropertyName = s.PropertyName,
                BirimAdi = s.UnitName,
                KalanGun = s.KalanGun,
                EndDate = s.EndDate
            }).ToList();

        vm.BosBirimler = bosBirimler
            .Take(5)
            .Select(b => new BosBirimOzet
            {
                BirimId = b.Id,
                PropertyName = b.PropertyName,
                BirimAdi = b.Name,
                Ilce = b.District,
                Yuzolcumu = b.Area
            }).ToList();

        if (User.HasModuleAccess("Internal.Payment"))
        {
            vm.HasOdemeAccess = true;
            await _chargeService.UpdateDelaysAsync();
            var tahakkuklar = await _chargeService.GetListAsync(propertyIds: propertyIds);
            var buAyTahakkuklar = tahakkuklar.Where(t => t.PeriodStart.Year == now.Year && t.PeriodStart.Month == now.Month).ToList();

            vm.BuAyBeklenenTahsilat = buAyTahakkuklar.Sum(t => t.TotalAmount);
            vm.BuAyTahsilEdilen = buAyTahakkuklar.Sum(t => t.PaidAmount);
            vm.GecikmisTahakkukAdet = tahakkuklar.Count(t => t.Status == ChargeStatus.Overdue);
            vm.GecikmisTutarToplam = tahakkuklar.Where(t => t.Status == ChargeStatus.Overdue).Sum(t => t.TotalAmount - t.PaidAmount);

            var odemeler = await _paymentService.GetAllAsync(propertyIds: propertyIds);
            vm.OnayBekleyenOdemeAdet = odemeler.Count(o => o.Status == PaymentStatus.PendingApproval);

            var eslesmemisler = await _bankTransactionService.GetAllAsync(BankMatchStatus.Unmatched);
            vm.EslesmemisHareketAdet = eslesmemisler.Count;

            vm.BuAyManuelBorcToplami = buAyTahakkuklar
                .Where(t => t.SourceType == ChargeSourceType.Manual && t.Status != ChargeStatus.Cancelled)
                .Sum(t => t.TotalAmount);
            vm.BuAyRezervasyonGeliri = buAyTahakkuklar
                .Where(t => t.SourceType == ChargeSourceType.Reservation && t.Status != ChargeStatus.Cancelled)
                .Sum(t => t.TotalAmount);

            var rezervasyonlar = await _reservationService.GetAllAsync(propertyIds);
            vm.TahakkukaAktarilmamisRezervasyonAdet = rezervasyonlar
                .Count(r => r.Status == ReservationStatus.Planned && r.TotalAmount > 0 && r.ChargeId == null);

            // --- Redesign metrikleri ---
            // Son 6 ay nakit akışı + tahsilat oranı sparkline
            var sonAltiAyBaslangic = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            var aylikGroup = tahakkuklar
                .Where(t => t.PeriodStart >= sonAltiAyBaslangic && t.Status != ChargeStatus.Cancelled)
                .GroupBy(t => new { t.PeriodStart.Year, t.PeriodStart.Month })
                .ToDictionary(
                    g => (g.Key.Year, g.Key.Month),
                    g => (Beklenen: g.Sum(t => t.TotalAmount), Odenen: g.Sum(t => t.PaidAmount)));

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
                .Where(t => t.DueDate >= otuzGunOnce && t.DueDate <= today && t.Status != ChargeStatus.Cancelled)
                .ToList();
            var bek30 = son30.Sum(t => t.TotalAmount);
            var od30 = son30.Sum(t => t.PaidAmount);
            vm.TahsilatOrani30Gun = bek30 > 0 ? Math.Round(od30 / bek30 * 100m, 1) : 0m;

            // Momentum — bu ay vs geçen ay (beklenen tahsilat üzerinden)
            var gecenAyStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var gecenAyEnd = gecenAyStart.AddMonths(1).AddDays(-1);
            vm.AylikGelirGecenAy = tahakkuklar
                .Where(t => t.PeriodStart >= gecenAyStart && t.PeriodStart <= gecenAyEnd && t.Status != ChargeStatus.Cancelled)
                .Sum(t => t.TotalAmount);
            vm.AylikGelirDelta = vm.AylikGelirGecenAy > 0
                ? Math.Round((vm.BuAyBeklenenTahsilat - vm.AylikGelirGecenAy) / vm.AylikGelirGecenAy * 100m, 1)
                : 0m;

            // Bugün vade dolan
            var bugun = tahakkuklar.Where(t => t.DueDate.Date == today &&
                (t.Status == ChargeStatus.Pending ||
                 t.Status == ChargeStatus.PartiallyPaid ||
                 t.Status == ChargeStatus.Overdue)).ToList();
            vm.BugunVadeDolanAdet = bugun.Count;
            vm.BugunVadeDolanTutar = bugun.Sum(t => t.TotalAmount - t.PaidAmount);

            // Top 5 gelir getiren taşınmaz (son 12 ay charge dönemleri, ödenen tutara göre)
            var sonYil = today.AddYears(-1);
            var birimSayisiByTasinmaz = tasinmazlar.ToDictionary(x => x.Id, x => x.BirimSayisi);
            vm.TopGelirTasinmaz = tahakkuklar
                .Where(t => t.PeriodStart >= sonYil && t.PropertyId != null && t.PaidAmount > 0)
                .GroupBy(t => new { TasinmazId = t.PropertyId!.Value, TasinmazAd = t.PropertyName ?? "—" })
                .Select(g => new DashboardGelirTasinmaz
                {
                    TasinmazId = g.Key.TasinmazId,
                    TasinmazAd = g.Key.TasinmazAd,
                    ToplamTahsilat = g.Sum(t => t.PaidAmount),
                    BirimSayisi = birimSayisiByTasinmaz.TryGetValue(g.Key.TasinmazId, out var bs) ? bs : 0
                })
                .OrderByDescending(x => x.ToplamTahsilat)
                .Take(5)
                .ToList();

            vm.TopGelirKiraci = tahakkuklar
                .Where(t => t.PeriodStart >= sonYil && t.PaidAmount > 0)
                .GroupBy(t => new { KiraciId = t.TenantId, KiraciAd = (t.TenantDisplayName ?? "—") })
                .Select(g => new DashboardGelirKiraci
                {
                    KiraciId = g.Key.KiraciId,
                    KiraciAd = g.Key.KiraciAd,
                    ToplamTahsilat = g.Sum(t => t.PaidAmount),
                    SozlesmeSayisi = g.Select(t => t.LeaseId).Distinct().Count()
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
