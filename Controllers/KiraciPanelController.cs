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
[Route("Tenant/Panel")]
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
        var tenantId = _currentUser.KiraciId!.Value;
        var today = DateTime.Today;
        var trCulture = CultureInfo.GetCultureInfo("tr-TR");

        var kiraci = await _db.Tenants.FirstOrDefaultAsync(k => k.Id == tenantId);
        if (kiraci == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);

        var rol = User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.TenantPortal.System.User.Invite)
            ? "Firma Yetkilisi"
            : User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.TenantPortal.Payment.Module)
                ? "Finans Yetkilisi"
                : "Kiracı";

        // Açık (ödenmemiş) tahakkuklar — paylaşılan baz sorgu
        var acikTahakkuklarBase = _db.Charges.Where(t =>
            t.Status == ChargeStatus.Pending ||
            t.Status == ChargeStatus.PartiallyPaid ||
            t.Status == ChargeStatus.Overdue);

        // KPI hesaplamaları
        var aktifSozlesmeAdedi = await _db.Leases
            .CountAsync(s => s.TenantId == tenantId && s.Status == LeaseStatus.Active);

        var toplamAcikBorc = await acikTahakkuklarBase
            .SumAsync(t => (decimal?)(t.TotalAmount - t.PaidAmount)) ?? 0m;

        var gecikmeEsigi = today;
        var yaklasanEsigi = today.AddDays(7);

        var yaklasanList = await acikTahakkuklarBase
            .Where(t => t.DueDate >= gecikmeEsigi && t.DueDate <= yaklasanEsigi)
            .Select(t => new { t.TotalAmount, t.PaidAmount })
            .ToListAsync();
        var yaklasanOdemeAdet = yaklasanList.Count;
        var yaklasanOdemeTutar = yaklasanList.Sum(x => x.TotalAmount - x.PaidAmount);

        var gecikmisList = await acikTahakkuklarBase
            .Where(t => t.DueDate < gecikmeEsigi)
            .Select(t => new { t.TotalAmount, t.PaidAmount })
            .ToListAsync();
        var gecikmisAdet = gecikmisList.Count;
        var gecikmisTutar = gecikmisList.Sum(x => x.TotalAmount - x.PaidAmount);

        // Son 6 ay nakit akışı — beklenen (vade) ve onaylı ödeme tutarı
        var sonAltiAyBaslangic = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

        var beklenenAylik = await _db.Charges
            .Where(t => t.DueDate >= sonAltiAyBaslangic)
            .GroupBy(t => new { t.DueDate.Year, t.DueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Toplam = g.Sum(t => t.TotalAmount) })
            .ToListAsync();

        var odenenAylik = await _db.PaymentAllocations
            .Where(o => o.Status == PaymentStatus.Approved && o.PaymentDate >= sonAltiAyBaslangic)
            .GroupBy(o => new { o.PaymentDate.Year, o.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Toplam = g.Sum(o => o.Amount) })
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
        var borcDagilimRaw = await _db.ChargeLineItems
            .Where(k => k.Charge.Status == ChargeStatus.Pending ||
                        k.Charge.Status == ChargeStatus.PartiallyPaid ||
                        k.Charge.Status == ChargeStatus.Overdue)
            .GroupBy(k => k.ChargeType.Name)
            .Select(g => new { Ad = g.Key, Amount = g.Sum(k => k.TotalAmount) })
            .ToListAsync();
        var borcDagilim = borcDagilimRaw
            .OrderByDescending(x => x.Amount)
            .Select(x => new KiraciPanelBorcDilim { Ad = x.Ad, Amount = x.Amount })
            .ToList();

        // Borç bakiyesi sparkline (son 6 ay sonu kümülatif kalan)
        var sparkline = new List<decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var ayBitis = new DateTime(today.Year, today.Month, 1).AddMonths(-i + 1).AddDays(-1);
            var bakiye = await _db.Charges
                .Where(t => t.DueDate <= ayBitis &&
                            (t.Status == ChargeStatus.Pending ||
                             t.Status == ChargeStatus.PartiallyPaid ||
                             t.Status == ChargeStatus.Overdue))
                .SumAsync(t => (decimal?)(t.TotalAmount - t.PaidAmount)) ?? 0m;
            sparkline.Add(bakiye);
        }

        // Yaklaşan tahakkuklar (5 satır — gecikmiş üstte, sonra vade en yakın)
        var yaklasanTahakkukRaw = await acikTahakkuklarBase
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new
            {
                t.Id,
                t.PeriodStart,
                t.DueDate,
                Kalan = t.TotalAmount - t.PaidAmount,
                TasinmazAd = t.Unit.Property.Name,
                BirimAd = t.Unit.Name
            })
            .ToListAsync();
        var yaklasanTahakkuklar = yaklasanTahakkukRaw.Select(t =>
        {
            var gunFarki = (t.DueDate.Date - today).Days;
            string renk = gunFarki < 0 ? "red" : gunFarki <= 7 ? "amber" : "emerald";
            return new KiraciPanelYaklasanItem
            {
                ChargeId = t.Id,
                Donem = t.PeriodStart.ToString("MMMM yyyy", trCulture),
                BirimAd = string.IsNullOrEmpty(t.BirimAd) ? (t.TasinmazAd ?? "—") : $"{t.BirimAd} · {t.TasinmazAd}",
                DueDate = t.DueDate,
                GunFarki = gunFarki,
                Kalan = t.Kalan,
                BorderRenk = renk
            };
        }).ToList();

        // Son ödemeler (5 satır)
        var sonOdemelerRaw = await _db.PaymentAllocations
            .OrderByDescending(o => o.PaymentDate)
            .Take(5)
            .Select(o => new { o.Id, o.PaymentDate, o.Amount, o.PaymentChannel, o.Status })
            .ToListAsync();
        var sonOdemeler = sonOdemelerRaw.Select(o => new KiraciPanelSonOdemeItem
        {
            OdemeId = o.Id,
            PaymentDate = o.PaymentDate,
            Amount = o.Amount,
            KanalAd = o.PaymentChannel switch
            {
                PaymentChannel.BankTransfer => "Havale",
                PaymentChannel.Eft => "EFT",
                PaymentChannel.Cash => "Nakit",
                _ => "Diğer"
            },
            DurumAd = o.Status switch
            {
                PaymentStatus.Approved => "Onaylandı",
                PaymentStatus.Rejected => "Reddedildi",
                _ => "Onay Bekliyor"
            },
            DurumDotRenk = o.Status switch
            {
                PaymentStatus.Approved => "emerald",
                PaymentStatus.Rejected => "red",
                _ => "amber"
            }
        }).ToList();

        var vm = new KiraciPanelViewModel
        {
            KiraciAd = kiraci.DisplayName,
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
