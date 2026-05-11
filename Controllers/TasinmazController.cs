using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Controllers;

[Authorize]
public class TasinmazController : Controller
{
    private readonly ITasinmazService _tasinmazService;
    private readonly ITasinmazFiyatService _tasinmazFiyatService;
    private readonly IIstatistikService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;
    private readonly ITarifeHiyerarsiService _tarifeHiyerarsisi;

    public TasinmazController(
        ITasinmazService tasinmazService,
        ITasinmazFiyatService tasinmazFiyatService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx,
        ITarifeHiyerarsiService tarifeHiyerarsisi)
    {
        _tasinmazService = tasinmazService;
        _tasinmazFiyatService = tasinmazFiyatService;
        _istatistik = istatistik;
        _userManager = userManager;
        _ctx = ctx;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.View)]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole("Goruntuleyici") ? userId : null;
        var tasinmazlar = await _tasinmazService.GetAllAsync(filterUserId);
        return View(tasinmazlar);
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.View)]
    public async Task<IActionResult> Detay(int id)
    {
        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var tasinmazlar = await _tasinmazService.GetAllAsync(userId);
            if (!tasinmazlar.Any(t => t.Id == id)) return Forbid();
        }

        var t = await _tasinmazService.GetByIdAsync(id);
        if (t == null) return NotFound();

        var birimler = t.Birimler.Select(b => new BirimDetayViewModel
        {
            Birim = b,
            Durum = _istatistik.GetBirimDurumu(b),
            AktifSozlesme = _istatistik.GetAktifSozlesme(b)
        }).ToList();

        var rezBirimIds = birimler
            .Where(b => b.Birim.BirimTuru?.RezervasyonYapilabilirMi == true)
            .Select(b => b.Birim.Id)
            .ToList();

        if (rezBirimIds.Count > 0)
        {
            var ozelKurallar = await _ctx.RezervasyonUcretKurallari
                .Where(r => r.BirimId != null && rezBirimIds.Contains(r.BirimId.Value))
                .ToListAsync();
            var globalKural = await _ctx.RezervasyonUcretKurallari
                .FirstOrDefaultAsync(r => r.BirimId == null && r.Aktif);

            foreach (var b in birimler.Where(b => b.Birim.BirimTuru?.RezervasyonYapilabilirMi == true))
                b.RezKural = ozelKurallar.FirstOrDefault(r => r.BirimId == b.Birim.Id) ?? globalKural;
        }

        var tumBirimIds = birimler.Select(b => b.Birim.Id).ToList();

        var birimOzelFiyatlari = new List<BirimOzelFiyatOzeti>();
        if (tumBirimIds.Count > 0)
        {
            var birimRateler = await _ctx.BirimRateler
                .Include(r => r.KiraciKategori)
                .Include(r => r.BorcTipi)
                .Where(r => tumBirimIds.Contains(r.BirimId))
                .OrderBy(r => r.KiraciKategori.Sira)
                .ThenBy(r => r.BorcTipi.Sira)
                .ToListAsync();

            birimOzelFiyatlari = birimler
                .Where(b => b.Birim.BirimTuru?.KiralanabilirMi == true)
                .Select(b => new BirimOzelFiyatOzeti
                {
                    Birim = b.Birim,
                    Rateler = birimRateler.Where(r => r.BirimId == b.Birim.Id).ToList()
                })
                .Where(b => b.Rateler.Any())
                .ToList();
        }

        var rezervasyonlar = await _ctx.ToplantiSalonuRezervasyonlari
            .Include(r => r.Birim)
            .Include(r => r.Kiraci)
            .Where(r => tumBirimIds.Contains(r.BirimId))
            .OrderByDescending(r => r.BaslangicTarihi)
            .ToListAsync();

        var globalRezKural = await _ctx.RezervasyonUcretKurallari
            .FirstOrDefaultAsync(r => r.BirimId == null && r.Aktif);

        var birimRezKurallari = rezBirimIds.Count > 0
            ? await _ctx.RezervasyonUcretKurallari
                .Where(r => r.BirimId != null && rezBirimIds.Contains(r.BirimId.Value))
                .ToListAsync()
            : new List<RezervasyonUcretKural>();

        var vm = new TasinmazDetayViewModel
        {
            Tasinmaz = t,
            Birimler = birimler,
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(id, pageSize: 100),
            Rezervasyonlar = rezervasyonlar,
            GlobalRezervasyonKural = globalRezKural,
            BirimRezervasyonKurallari = birimRezKurallari,
            BirimOzelFiyatlari = birimOzelFiyatlari
        };
        return View(vm);
    }

    private async Task PopulateViewBagAsync()
    {
        var birimTurleri = await _ctx.BirimTurleri.Where(b => b.Aktif).OrderBy(b => b.Sira).ToListAsync();
        ViewBag.BirimTurleri = birimTurleri.Where(b => b.KiralanabilirMi).ToList();
        ViewBag.RezervasyonBirimTurleri = birimTurleri.Where(b => b.RezervasyonYapilabilirMi).ToList();
        ViewBag.TasinmazTipleri = await _ctx.TasinmazTipleri.Where(t => t.Aktif).OrderBy(t => t.Sira).ToListAsync();
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle()
    {
        await PopulateViewBagAsync();
        var vm = new TasinmazEkleViewModel
        {
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(0, pageSize: 100),
            ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Tasinmaz, yil: DateTime.Now.Year)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle(TasinmazEkleViewModel vm)
    {
        if (vm.KiralamaSekli == KiralamaSekli.BirimBazli)
        {
            if (vm.Ofisler == null || vm.Ofisler.Count == 0)
            {
                ModelState.AddModelError("Ofisler", "Birim bazlı kiralama için en az bir birim eklemelisiniz.");
            }
            else
            {
                for (int i = 0; i < vm.Ofisler.Count; i++)
                {
                    var ofis = vm.Ofisler[i];
                    
                    if (string.IsNullOrWhiteSpace(ofis.OfisNo))
                        ModelState.AddModelError($"Ofisler[{i}].OfisNo", "Birim No zorunludur.");
                    
                    if (ofis.BirimTuruId == null || ofis.BirimTuruId <= 0)
                        ModelState.AddModelError($"Ofisler[{i}].BirimTuruId", "Birim Türü zorunludur.");
                        
                    if (ofis.Yuzolcumu <= 0)
                        ModelState.AddModelError($"Ofisler[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
                }

                // Tekrarlayan kontrolü (sadece OfisNo dolu olanlar için)
                var tekrarlayanOfisNo = vm.Ofisler
                    .Where(o => !string.IsNullOrWhiteSpace(o.OfisNo))
                    .GroupBy(o => o.OfisNo.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (tekrarlayanOfisNo != null)
                    ModelState.AddModelError("Ofisler", $"Birim No '{tekrarlayanOfisNo}' aynı taşınmaz içinde tekrar kullanılamaz.");
            }
        }
        else
        {
            vm.Ofisler.Clear();
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();
            return View(vm);
        }

        var t = new Tasinmaz
        {
            Ad = vm.Ad,
            TasinmazTipiId = vm.TasinmazTipiId,
            KiralamaSekli = vm.KiralamaSekli,
            Il = vm.Il,
            Ilce = vm.Ilce,
            Mahalle = vm.Mahalle,
            AcikAdres = vm.AcikAdres,
            AcikYuzolcumu = vm.AcikYuzolcumu,
            KapaliYuzolcumu = vm.KapaliYuzolcumu,
            KatSayisi = vm.KatSayisi,
            Aciklama = vm.Aciklama
        };

        var rezervasyonAlanlari = vm.RezervasyonAlanlari
            .Where(r => !string.IsNullOrWhiteSpace(r.Ad))
            .ToList();

        await _tasinmazService.CreateAsync(t,
            vm.Ofisler.Count > 0 ? vm.Ofisler : null,
            rezervasyonAlanlari.Count > 0 ? rezervasyonAlanlari : null);

        // Fiyat Matrisini Kaydet
        var userId = _userManager.GetUserId(User);
        await _tasinmazFiyatService.SaveMatrisiAsync(t.Id, vm.FiyatMatrisi, userId ?? "");

        TempData["Success"] = $"'{t.Ad}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}
