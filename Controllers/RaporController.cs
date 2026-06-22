using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.Odeme.View)]
public class RaporController : Controller
{
    private readonly ITahakkukService _tahakkukService;
    private readonly IYetkiKapsamiProvider _provider;

    public RaporController(ITahakkukService tahakkukService, IYetkiKapsamiProvider provider)
    {
        _tahakkukService = tahakkukService;
        _provider = provider;
    }

    public async Task<IActionResult> Index(int? yil)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        int secilenYil = yil ?? DateTime.Today.Year;
        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        var tahakkuklar = await _tahakkukService.GetListAsync(tasinmazIds: tasinmazIds);

        var trCulture = new CultureInfo("tr-TR");

        var satirlar = Enumerable.Range(1, 12).Select(ay =>
        {
            var ayTahakkuklar = tahakkuklar.Where(t => t.DonemBaslangic.Year == secilenYil && t.DonemBaslangic.Month == ay).ToList();
            var gecikmisler = ayTahakkuklar.Where(t => t.Durum == TahakkukDurumu.Gecikti).ToList();
            return new AylikRaporSatir
            {
                Ay = ay,
                AyAdi = new DateTime(secilenYil, ay, 1).ToString("MMMM", trCulture),
                TahakkukSayisi = ayTahakkuklar.Count,
                Beklenen = ayTahakkuklar.Sum(t => t.ToplamTutar),
                TahsilEdilen = ayTahakkuklar.Sum(t => t.OdenenTutar),
                GecikmisTahakkukAdet = gecikmisler.Count,
                GecikmisTutar = gecikmisler.Sum(t => t.ToplamTutar - t.OdenenTutar)
            };
        }).ToList();

        var vm = new AylikRaporViewModel { Yil = secilenYil, Satirlar = satirlar };

        var mevcutYillar = tahakkuklar.Select(t => t.DonemBaslangic.Year).Distinct().OrderByDescending(y => y).ToList();
        if (!mevcutYillar.Contains(secilenYil)) mevcutYillar.Insert(0, secilenYil);
        ViewBag.MevcutYillar = mevcutYillar;

        return View(vm);
    }
}
