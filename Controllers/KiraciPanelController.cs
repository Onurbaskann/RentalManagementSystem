using System.Globalization;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[RequireKiraciId]
[Route("Kiraci/Panel")]
public class KiraciPanelController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public KiraciPanelController(ApplicationDbContext db, ICurrentUserContext currentUser, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var today = DateTime.Today;
        var trCulture = CultureInfo.GetCultureInfo("tr-TR");

        var kiraci = await _db.Kiraciler.FirstOrDefaultAsync(k => k.Id == kiraciId);
        if (kiraci == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);

        var rol = User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.TenantPortal.System.User.Invite)
            ? "Firma Yetkilisi"
            : User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.TenantPortal.Payment.Module)
                ? "Finans Yetkilisi"
                : "Kiracı";

        // Açık (ödenmemiş) tahakkuklar — paylaşılan baz sorgu
        var acikTahakkuklarBase = _db.Tahakkuklar.Where(t =>
            t.Durum == ChargeStatus.Pending ||
            t.Durum == ChargeStatus.PartiallyPaid ||
            t.Durum == ChargeStatus.Overdue);

        // KPI hesaplamaları
        var aktifSozlesmeAdedi = await _db.Sozlesmeler
            .CountAsync(s => s.KiraciId == kiraciId && s.Durum == LeaseStatus.Active);

        var toplamAcikBorc = await acikTahakkuklarBase
            .SumAsync(t => (decimal?)(t.ToplamTutar - t.OdenenTutar)) ?? 0m;

        var gecikmeEsigi = today;
        var yaklasanEsigi = today.AddDays(7);

        var yaklasanList = await acikTahakkuklarBase
            .Where(t => t.VadeTarihi >= gecikmeEsigi && t.VadeTarihi <= yaklasanEsigi)
            .Select(t => new { t.ToplamTutar, t.OdenenTutar })
            .ToListAsync();
        var yaklasanOdemeAdet = yaklasanList.Count;
        var yaklasanOdemeTutar = yaklasanList.Sum(x => x.ToplamTutar - x.OdenenTutar);

        var gecikmisList = await acikTahakkuklarBase
            .Where(t => t.VadeTarihi < gecikmeEsigi)
            .Select(t => new { t.ToplamTutar, t.OdenenTutar })
            .ToListAsync();
        var gecikmisAdet = gecikmisList.Count;
        var gecikmisTutar = gecikmisList.Sum(x => x.ToplamTutar - x.OdenenTutar);

        // Son 6 ay nakit akışı — beklenen (vade) ve onaylı ödeme tutarı
        var sonAltiAyBaslangic = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

        var beklenenAylik = await _db.Tahakkuklar
            .Where(t => t.VadeTarihi >= sonAltiAyBaslangic)
            .GroupBy(t => new { t.VadeTarihi.Year, t.VadeTarihi.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Toplam = g.Sum(t => t.ToplamTutar) })
            .ToListAsync();

        var odenenAylik = await _db.TahakkukOdemeler
            .Where(o => o.Durum == PaymentStatus.Approved && o.OdemeTarihi >= sonAltiAyBaslangic)
            .GroupBy(o => new { o.OdemeTarihi.Year, o.OdemeTarihi.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Toplam = g.Sum(o => o.Tutar) })
            .ToListAsync();

        var aylikNakit = new List<KiraciPanelAylikNakit>();
        for (int i = 5; i >= 0; i--)
        {
            var ay = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            var bek = beklenenAylik.FirstOrDefault(x => x.Year == ay.Year && x.Month == ay.Month)?.Toplam ?? 0m;
            var od = odenenAylik.FirstOrDefault(x => x.Year == ay.Year && x.Month == ay.Month)?.Toplam ?? 0m;
            aylikNakit.Add(new KiraciPanelAylikNakit
            {
                AyEtiket = trCulture.DateTimeFormat.GetAbbreviatedMonthName(ay.Month),
                Beklenen = bek,
                Odenen = od
            });
        }

        // Borç tipi dağılımı (açık tahakkuk kalemleri)
        var borcDagilimRaw = await _db.TahakkukKalemleri
            .Where(k => k.Tahakkuk.Durum == ChargeStatus.Pending ||
                        k.Tahakkuk.Durum == ChargeStatus.PartiallyPaid ||
                        k.Tahakkuk.Durum == ChargeStatus.Overdue)
            .GroupBy(k => k.BorcTipi.Ad)
            .Select(g => new { Ad = g.Key, Tutar = g.Sum(k => k.ToplamTutar) })
            .ToListAsync();
        var borcDagilim = borcDagilimRaw
            .OrderByDescending(x => x.Tutar)
            .Select(x => new KiraciPanelBorcDilim { Ad = x.Ad, Tutar = x.Tutar })
            .ToList();

        // Borç bakiyesi sparkline (son 6 ay sonu kümülatif kalan)
        var sparkline = new List<decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var ayBitis = new DateTime(today.Year, today.Month, 1).AddMonths(-i + 1).AddDays(-1);
            var bakiye = await _db.Tahakkuklar
                .Where(t => t.VadeTarihi <= ayBitis &&
                            (t.Durum == ChargeStatus.Pending ||
                             t.Durum == ChargeStatus.PartiallyPaid ||
                             t.Durum == ChargeStatus.Overdue))
                .SumAsync(t => (decimal?)(t.ToplamTutar - t.OdenenTutar)) ?? 0m;
            sparkline.Add(bakiye);
        }

        // Yaklaşan tahakkuklar (5 satır — gecikmiş üstte, sonra vade en yakın)
        var yaklasanTahakkukRaw = await acikTahakkuklarBase
            .OrderBy(t => t.VadeTarihi)
            .Take(5)
            .Select(t => new
            {
                t.Id,
                t.DonemBaslangic,
                t.VadeTarihi,
                Kalan = t.ToplamTutar - t.OdenenTutar,
                TasinmazAd = t.Birim.Tasinmaz.Ad,
                BirimAd = t.Birim.Ad
            })
            .ToListAsync();
        var yaklasanTahakkuklar = yaklasanTahakkukRaw.Select(t =>
        {
            var gunFarki = (t.VadeTarihi.Date - today).Days;
            string renk = gunFarki < 0 ? "red" : gunFarki <= 7 ? "amber" : "emerald";
            return new KiraciPanelYaklasanItem
            {
                TahakkukId = t.Id,
                Donem = t.DonemBaslangic.ToString("MMMM yyyy", trCulture),
                BirimAd = string.IsNullOrEmpty(t.BirimAd) ? (t.TasinmazAd ?? "—") : $"{t.BirimAd} · {t.TasinmazAd}",
                VadeTarihi = t.VadeTarihi,
                GunFarki = gunFarki,
                Kalan = t.Kalan,
                BorderRenk = renk
            };
        }).ToList();

        // Son ödemeler (5 satır)
        var sonOdemelerRaw = await _db.TahakkukOdemeler
            .OrderByDescending(o => o.OdemeTarihi)
            .Take(5)
            .Select(o => new { o.Id, o.OdemeTarihi, o.Tutar, o.PaymentChannel, o.Durum })
            .ToListAsync();
        var sonOdemeler = sonOdemelerRaw.Select(o => new KiraciPanelSonOdemeItem
        {
            OdemeId = o.Id,
            OdemeTarihi = o.OdemeTarihi,
            Tutar = o.Tutar,
            KanalAd = o.PaymentChannel switch
            {
                PaymentChannel.BankTransfer => "Havale",
                PaymentChannel.Eft => "EFT",
                PaymentChannel.Cash => "Nakit",
                _ => "Diğer"
            },
            DurumAd = o.Durum switch
            {
                PaymentStatus.Approved => "Onaylandı",
                PaymentStatus.Rejected => "Reddedildi",
                _ => "Onay Bekliyor"
            },
            DurumDotRenk = o.Durum switch
            {
                PaymentStatus.Approved => "emerald",
                PaymentStatus.Rejected => "red",
                _ => "amber"
            }
        }).ToList();

        var vm = new KiraciPanelViewModel
        {
            KiraciAd = kiraci.GosterimAdi,
            KullaniciAd = user?.AdSoyad ?? user?.Email ?? "Kullanıcı",
            KullaniciRol = rol,
            TarihEtiket = today.ToString("d MMMM yyyy, dddd", trCulture),
            AktifSozlesmeAdedi = aktifSozlesmeAdedi,
            ToplamAcikBorc = toplamAcikBorc,
            YaklasanOdemeAdet = yaklasanOdemeAdet,
            YaklasanOdemeTutar = yaklasanOdemeTutar,
            GecikmisAdet = gecikmisAdet,
            GecikmisTutar = gecikmisTutar,
            AylikNakit = aylikNakit,
            BorcTipiDagilimi = borcDagilim,
            BorcBakiyesiSparkline = sparkline,
            YaklasanTahakkuklar = yaklasanTahakkuklar,
            SonOdemeler = sonOdemeler
        };

        return View(vm);
    }
}
