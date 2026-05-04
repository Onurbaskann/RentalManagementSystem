using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using System.Globalization;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.Odeme.View)]
public class RaporController : Controller
{
    private readonly ITahakkukService _tahakkukService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RaporController(ITahakkukService tahakkukService, UserManager<ApplicationUser> userManager)
    {
        _tahakkukService = tahakkukService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? yil)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        int secilenYil = yil ?? DateTime.Today.Year;
        var userId = User.IsInRole("Goruntuleyici") ? _userManager.GetUserId(User) : null;
        var tahakkuklar = await _tahakkukService.GetAllAsync(userId: userId);

        var trCulture = new CultureInfo("tr-TR");

        var satirlar = Enumerable.Range(1, 12).Select(ay => {
            var ayTahakkuklar = tahakkuklar.Where(t => t.DonemBaslangic.Year == secilenYil && t.DonemBaslangic.Month == ay).ToList();
            return new AylikRaporSatir {
                Ay = ay,
                AyAdi = new DateTime(secilenYil, ay, 1).ToString("MMMM", trCulture),
                TahakkukSayisi = ayTahakkuklar.Count,
                Beklenen = ayTahakkuklar.Sum(t => t.ToplamTutar),
                TahsilEdilen = ayTahakkuklar.Sum(t => t.OdenenTutar),
                GecikmisTahakkukAdet = ayTahakkuklar.Count(t => t.Durum == TahakkukDurumu.Gecikti)
            };
        }).ToList();

        var vm = new AylikRaporViewModel { Yil = secilenYil, Satirlar = satirlar };

        var mevcutYillar = tahakkuklar.Select(t => t.DonemBaslangic.Year).Distinct().OrderByDescending(y => y).ToList();
        if (!mevcutYillar.Contains(secilenYil)) mevcutYillar.Insert(0, secilenYil);
        ViewBag.MevcutYillar = mevcutYillar;

        return View(vm);
    }
}
