using KiraTakip.Authorization;
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
        var filterUserId = User.IsInRole(RoleNames.Goruntuleyici) ? userId : null;
        var tasinmazlar = await _tasinmazService.GetAllAsync(filterUserId);
        return View(tasinmazlar);
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.View)]
    public async Task<IActionResult> Detay(int id)
    {
        if (User.IsInRole(RoleNames.Goruntuleyici))
        {
            var userId = _userManager.GetUserId(User);
            var tasinmazlar = await _tasinmazService.GetAllAsync(userId);
            if (!tasinmazlar.Any(t => t.Id == id)) return Forbid();
        }

        var t = await _tasinmazService.GetByIdAsync(id);
        if (t == null) return NotFound();

        var birimler = new List<BirimDetayViewModel>();
        foreach (var b in t.Birimler)
        {
            var aktifSozlesme = _istatistik.GetAktifSozlesme(b);
            birimler.Add(new BirimDetayViewModel
            {
                Birim = b,
                Durum = _istatistik.GetBirimDurumu(b),
                AktifSozlesme = aktifSozlesme,
                AylikBedel = aktifSozlesme != null ? await _istatistik.AylikBedelAsync(aktifSozlesme) : 0m
            });
        }

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

        var sozlesmeBedelleri = new Dictionary<int, decimal>();
        foreach (var s in t.Birimler.SelectMany(b => b.Sozlesmeler))
        {
            if (!sozlesmeBedelleri.ContainsKey(s.Id))
                sozlesmeBedelleri[s.Id] = await _istatistik.AylikBedelAsync(s);
        }

        var vm = new TasinmazDetayViewModel
        {
            Tasinmaz = t,
            Birimler = birimler,
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(id, pageSize: 100),
            Rezervasyonlar = rezervasyonlar,
            GlobalRezervasyonKural = globalRezKural,
            BirimRezervasyonKurallari = birimRezKurallari,
            BirimOzelFiyatlari = birimOzelFiyatlari,
            SozlesmeAylikBedelleri = sozlesmeBedelleri
        };
        return View(vm);
    }

    private async Task PopulateViewBagAsync()
    {
        var birimTurleri = await _ctx.BirimTurleri.Where(b => b.Aktif).OrderBy(b => b.Sira).ToListAsync();
        ViewBag.BirimTurleri = birimTurleri.Where(b => b.KiralanabilirMi).ToList();
        ViewBag.RezervasyonBirimTurleri = birimTurleri.Where(b => b.RezervasyonYapilabilirMi).ToList();
        ViewBag.TasinmazTipleri = await _ctx.TasinmazTipleri.Where(t => t.Aktif).OrderBy(t => t.Sira).ToListAsync();

        ViewBag.TasinmazTipiKiralamaSekilleri = await _ctx.TasinmazTipiKiralamaSekilleri
            .GroupBy(t => t.TasinmazTipiId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => (int)x.KiralamaSekli).OrderBy(x => x).ToArray());
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle()
    {
        await PopulateViewBagAsync();
        var vm = new TasinmazEkleViewModel
        {
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(0, pageSize: 100),
            ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Tasinmaz, yil: DateTime.Now.Year),
            ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Create)]
    public async Task<IActionResult> Ekle(TasinmazEkleViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Ad))
            ModelState.AddModelError("Ad", "Taşınmaz adı zorunludur.");
        if (vm.TasinmazTipiId == null || vm.TasinmazTipiId <= 0)
            ModelState.AddModelError("TasinmazTipiId", "Taşınmaz tipi zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Il))
            ModelState.AddModelError("Il", "İl zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Ilce))
            ModelState.AddModelError("Ilce", "İlçe zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Mahalle))
            ModelState.AddModelError("Mahalle", "Mahalle zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.AcikAdres))
            ModelState.AddModelError("AcikAdres", "Açık adres zorunludur.");

        if (vm.TasinmazTipiId != null && vm.TasinmazTipiId > 0)
        {
            var izinli = await _ctx.TasinmazTipiKiralamaSekilleri
                .Where(t => t.TasinmazTipiId == vm.TasinmazTipiId.Value)
                .Select(t => t.KiralamaSekli)
                .ToListAsync();

            if (izinli.Count > 0 && !izinli.Contains(vm.KiralamaSekli))
                ModelState.AddModelError("KiralamaSekli", "Seçilen taşınmaz tipi bu kiralama şekline izin vermiyor.");
        }

        if (vm.KiralamaSekli == KiralamaSekli.BirimBazli)
        {
            if (vm.Birimler == null || vm.Birimler.Count == 0)
            {
                ModelState.AddModelError("Birimler", "Birim bazlı kiralama için en az bir birim eklemelisiniz.");
            }
            else
            {
                for (int i = 0; i < vm.Birimler.Count; i++)
                {
                    var birim = vm.Birimler[i];

                    if (string.IsNullOrWhiteSpace(birim.BirimNo))
                        ModelState.AddModelError($"Birimler[{i}].BirimNo", "Birim No zorunludur.");

                    if (birim.KatNo == null)
                        ModelState.AddModelError($"Birimler[{i}].KatNo", "Kat No zorunludur.");

                    if (birim.BirimTuruId == null || birim.BirimTuruId <= 0)
                        ModelState.AddModelError($"Birimler[{i}].BirimTuruId", "Birim Türü zorunludur.");

                    if (string.IsNullOrWhiteSpace(birim.Ad) && !string.IsNullOrWhiteSpace(birim.BirimNo))
                        birim.Ad = "Birim " + birim.BirimNo;
                    if (string.IsNullOrWhiteSpace(birim.Ad))
                        ModelState.AddModelError($"Birimler[{i}].Ad", "Ad zorunludur.");

                    if (birim.Yuzolcumu <= 0)
                        ModelState.AddModelError($"Birimler[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
                }

                var tekrarlayanBirimNo = vm.Birimler
                    .Where(b => !string.IsNullOrWhiteSpace(b.BirimNo))
                    .GroupBy(b => b.BirimNo.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (tekrarlayanBirimNo != null)
                    ModelState.AddModelError("Birimler", $"Birim No '{tekrarlayanBirimNo}' aynı taşınmaz içinde tekrar kullanılamaz.");
            }
        }
        else
        {
            vm.Birimler.Clear();
        }

        for (int i = 0; i < vm.RezervasyonAlanlari.Count; i++)
        {
            var alan = vm.RezervasyonAlanlari[i];
            if (string.IsNullOrWhiteSpace(alan.Ad))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Ad", "Alan Adı zorunludur.");
            if (alan.Yuzolcumu <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
            if (alan.BirimTuruId == null || alan.BirimTuruId <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimTuruId", "Alan Türü zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();

            var freshMatris = await _tasinmazFiyatService.GetMatrisiAsync(0, pageSize: 100);
            vm.FiyatMatrisi.Kolonlar = freshMatris.Kolonlar;
            if (vm.FiyatMatrisi.Satirlar == null || vm.FiyatMatrisi.Satirlar.Count == 0)
                vm.FiyatMatrisi.Satirlar = freshMatris.Satirlar;

            vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Tasinmaz, yil: DateTime.Now.Year);
            vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

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

        await _tasinmazService.CreateAsync(t,
            vm.Birimler.Count > 0 ? vm.Birimler : null,
            vm.RezervasyonAlanlari.Count > 0 ? vm.RezervasyonAlanlari : null);

        // Fiyat Matrisini Kaydet
        var userId = _userManager.GetUserId(User);
        await _tasinmazFiyatService.SaveMatrisiAsync(t.Id, vm.FiyatMatrisi, userId ?? "");

        TempData["Success"] = $"'{t.Ad}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}
