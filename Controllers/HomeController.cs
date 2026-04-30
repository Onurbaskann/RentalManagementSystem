using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services;

namespace KiraTakip.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly DummyDataService _data;
    private readonly IstatistikService _istatistik;
    private readonly UserTasinmazYetkiService _yetkiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        DummyDataService data, 
        IstatistikService istatistik,
        UserTasinmazYetkiService yetkiService,
        UserManager<ApplicationUser> userManager)
    {
        _data = data;
        _istatistik = istatistik;
        _yetkiService = yetkiService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var userId = _userManager.GetUserId(User);
        List<int> yetkiliIds = null!;

        if (User.IsInRole("Goruntuleyici"))
        {
            yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId!);
        }

        var tasinmazlar = _data.Tasinmazlar
            .Where(t => yetkiliIds == null || yetkiliIds.Contains(t.Id))
            .ToList();

        var tumBirimler = _data.GetTumBirimler()
            .Where(b => yetkiliIds == null || yetkiliIds.Contains(b.TasinmazId))
            .ToList();

        var sozlesmeler = _data.Sozlesmeler
            .Where(s => yetkiliIds == null || yetkiliIds.Contains(s.Birim.TasinmazId))
            .ToList();

        var vm = new DashboardViewModel
        {
            ToplamTasinmaz = tasinmazlar.Count,
            TipiDagilim = tasinmazlar.GroupBy(t => t.Tipi).ToDictionary(g => g.Key, g => g.Count()),
            ToplamBirim = tumBirimler.Count,
            AktifSozlesme = sozlesmeler.Count(_istatistik.Aktif),
            AylikToplamGelir = sozlesmeler.Where(_istatistik.Aktif).Sum(_istatistik.AylikBedel),
            YillikProj = sozlesmeler.Where(_istatistik.Aktif).Sum(_istatistik.YillikBedel),
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

        vm.BosBirimler = _data.GetBosBirimler()
            .Where(b => yetkiliIds == null || yetkiliIds.Contains(b.TasinmazId))
            .Take(5)
            .Select(b => new BosBirimOzet
            {
                BirimId = b.Id,
                TasinmazAdi = b.Tasinmaz.Ad,
                BirimAdi = b.Ad,
                Ilce = b.Tasinmaz.Ilce,
                Yuzolcumu = b.Yuzolcumu
            }).ToList();

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
