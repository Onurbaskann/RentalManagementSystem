using System.Diagnostics;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ITasinmazService tasinmazService,
        ISozlesmeService sozlesmeService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager)
    {
        _tasinmazService = tasinmazService;
        _sozlesmeService = sozlesmeService;
        _istatistik = istatistik;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole("Goruntuleyici") ? userId : null;

        var tasinmazlar = await _tasinmazService.GetAllAsync(filterUserId);
        var tumBirimler = tasinmazlar.SelectMany(t => t.Birimler).ToList();
        var sozlesmeler = await _sozlesmeService.GetAllAsync(userId: filterUserId);
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync(filterUserId);

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

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
