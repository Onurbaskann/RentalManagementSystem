using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.Payment.Module)]
public class ReportController : Controller
{
    private readonly IChargeService _chargeService;
    private readonly IPermissionScopeProvider _provider;

    public ReportController(IChargeService tahakkukService, IPermissionScopeProvider provider)
    {
        _chargeService = tahakkukService;
        _provider = provider;
    }

    public async Task<IActionResult> Index(int? yil)
    {
        await _chargeService.GecikmeleriGuncelleAsync();

        int secilenYil = yil ?? DateTime.Today.Year;
        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        var tahakkuklar = await _chargeService.GetListAsync(tasinmazIds: tasinmazIds);

        var trCulture = new CultureInfo("tr-TR");

        var satirlar = Enumerable.Range(1, 12).Select(ay =>
        {
            var ayTahakkuklar = tahakkuklar.Where(t => t.PeriodStart.Year == secilenYil && t.PeriodStart.Month == ay).ToList();
            var gecikmisler = ayTahakkuklar.Where(t => t.Durum == ChargeStatus.Overdue).ToList();
            return new AylikRaporSatir
            {
                Ay = ay,
                AyAdi = new DateTime(secilenYil, ay, 1).ToString("MMMM", trCulture),
                TahakkukSayisi = ayTahakkuklar.Count,
                Beklenen = ayTahakkuklar.Sum(t => t.ToplamTutar),
                TahsilEdilen = ayTahakkuklar.Sum(t => t.PaidAmount),
                GecikmisTahakkukAdet = gecikmisler.Count,
                GecikmisTutar = gecikmisler.Sum(t => t.ToplamTutar - t.PaidAmount)
            };
        }).ToList();

        var vm = new AylikRaporViewModel { Yil = secilenYil, Satirlar = satirlar };

        var mevcutYillar = tahakkuklar.Select(t => t.PeriodStart.Year).Distinct().OrderByDescending(y => y).ToList();
        if (!mevcutYillar.Contains(secilenYil)) mevcutYillar.Insert(0, secilenYil);
        ViewBag.MevcutYillar = mevcutYillar;

        return View(vm);
    }
}
