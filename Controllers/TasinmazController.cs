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

[Authorize]
public class TasinmazController : Controller
{
    private readonly ITasinmazService _tasinmazService;
    private readonly ITasinmazFiyatService _tasinmazFiyatService;
    private readonly IIstatistikService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;
    private readonly ITarifeHiyerarsiService _tarifeHiyerarsisi;
    private readonly IYetkiKapsamiProvider _provider;

    public TasinmazController(
        ITasinmazService tasinmazService,
        ITasinmazFiyatService tasinmazFiyatService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx,
        ITarifeHiyerarsiService tarifeHiyerarsisi,
        IYetkiKapsamiProvider provider)
    {
        _tasinmazService = tasinmazService;
        _tasinmazFiyatService = tasinmazFiyatService;
        _istatistik = istatistik;
        _userManager = userManager;
        _ctx = ctx;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.Module)]
    public async Task<IActionResult> Index()
    {
        var tasinmazlar = await _tasinmazService.GetAllAsync(_provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds);
        return View(tasinmazlar);
    }

    [Authorize(Policy = PermissionCatalog.Tasinmaz.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        if (!_provider.KapsamdaMi(id)) return Forbid();

        var t = await _tasinmazService.GetByIdAsync(id);
        if (t == null) return NotFound();

        var vm = new TasinmazDetayViewModel
        {
            Tasinmaz = t,
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(id, pageSize: 100)
        };
        return View(vm);
    }

    private async Task PopulateViewBagAsync()
    {
        var birimTurleri = await _ctx.BirimTurleri.Where(b => b.Aktif).OrderBy(b => b.Sira).ToListAsync();
        ViewBag.BirimTurleri = birimTurleri.Where(b => b.KiralanabilirMi).ToList();
        ViewBag.RezervasyonBirimTurleri = birimTurleri.Where(b => b.RezervasyonYapilabilirMi).ToList();
        var tipler = await _ctx.TasinmazTipleri.Where(k => k.Aktif).OrderBy(t => t.Sira).ToListAsync();
        ViewBag.TasinmazTipleri = tipler;
        ViewBag.TasinmazTipiKiralamaSekilleri = tipler.ToDictionary(
            t => t.Id,
            t =>
            {
                var list = new List<int>();
                if (t.TekParcaDestekli) list.Add((int)KiralamaSekli.TekParca);
                if (t.BirimBazliDestekli) list.Add((int)KiralamaSekli.BirimBazli);
                return list.ToArray();
            });
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

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Edit)]
    public async Task<IActionResult> Duzenle(int id)
    {
        if (!_provider.KapsamdaMi(id)) return Forbid();

        var vm = await _tasinmazService.GetForEditAsync(id);
        if (vm == null) return NotFound();

        vm.FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(id, pageSize: 100);
        vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Tasinmaz, yil: DateTime.Now.Year);
        vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

        await PopulateViewBagAsync();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tasinmaz.Edit)]
    public async Task<IActionResult> Duzenle(TasinmazDuzenleViewModel vm)
    {
        _provider.TasinmazGuard(vm.Id);

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

        for (int i = 0; i < vm.RezervasyonAlanlari.Count; i++)
        {
            var alan = vm.RezervasyonAlanlari[i];
            if (string.IsNullOrWhiteSpace(alan.BirimNo))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimNo", "Birim No zorunludur.");
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
            var freshMatris = await _tasinmazFiyatService.GetMatrisiAsync(vm.Id, pageSize: 100);
            if (vm.FiyatMatrisi?.Satirlar != null)
            {
                foreach (var freshSatir in freshMatris.Satirlar)
                {
                    var userSatir = vm.FiyatMatrisi.Satirlar.FirstOrDefault(s => s.KiraciKategoriId == freshSatir.KiraciKategoriId);
                    if (userSatir != null)
                    {
                        foreach (var freshHucre in freshSatir.Hucreler)
                        {
                            var userHucre = userSatir.Hucreler.FirstOrDefault(h => h.BorcTipiId == freshHucre.BorcTipiId);
                            if (userHucre != null)
                            {
                                freshHucre.BirimDeger = userHucre.BirimDeger;
                                freshHucre.HesaplamaYontemi = userHucre.HesaplamaYontemi;
                                freshHucre.KdvOrani = userHucre.KdvOrani;
                            }
                        }
                    }
                }
            }
            vm.FiyatMatrisi = freshMatris;

            vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Tasinmaz, yil: DateTime.Now.Year);
            vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

            // AktifSozlesmesiVar flag'leri POST round-trip'te sıfırlanıyor, DB'den tekrar doldur
            await RestoreAktifFlaglarAsync(vm);

            return View(vm);
        }

        await _tasinmazService.UpdateWithChildrenAsync(vm);

        var userId2 = _userManager.GetUserId(User);
        await _tasinmazFiyatService.SaveMatrisiAsync(vm.Id, vm.FiyatMatrisi, userId2 ?? "");

        TempData["Success"] = $"'{vm.Ad}' başarıyla güncellendi.";
        return RedirectToAction("Detay", new { id = vm.Id });
    }

    private async Task RestoreAktifFlaglarAsync(TasinmazDuzenleViewModel vm)
    {
        var now = DateTime.Now;
        foreach (var b in vm.Birimler.Where(b => b.Id.HasValue))
        {
            b.AktifSozlesmesiVar = await _ctx.Sozlesmeler
                .AnyAsync(s => s.BirimId == b.Id!.Value
                               && s.Durum == SozlesmeDurumu.Aktif
                               && s.BaslangicTarihi <= now
                               && s.BitisTarihi >= now);
        }

        var rezIds = vm.RezervasyonAlanlari.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToList();
        var aktifRezBirimIds = await _ctx.Rezervasyonlari
            .Where(r => rezIds.Contains(r.BirimId)
                        && r.Durum == RezervasyonDurumu.Planlandi
                        && r.BitisTarihi >= now)
            .Select(r => r.BirimId)
            .Distinct()
            .ToListAsync();

        foreach (var r in vm.RezervasyonAlanlari.Where(r => r.Id.HasValue))
            r.AktifRezervasyonuVar = aktifRezBirimIds.Contains(r.Id!.Value);
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
            var tip = await _ctx.TasinmazTipleri.FirstOrDefaultAsync(k => k.Id == vm.TasinmazTipiId.Value);
            if (tip != null)
            {
                var secimIzinli = vm.KiralamaSekli == KiralamaSekli.TekParca ? tip.TekParcaDestekli : tip.BirimBazliDestekli;
                if (!secimIzinli)
                    ModelState.AddModelError("KiralamaSekli", "Seçilen taşınmaz tipi bu kiralama şekline izin vermiyor.");
            }
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
            if (string.IsNullOrWhiteSpace(alan.BirimNo))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimNo", "Birim No zorunludur.");
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
            if (vm.FiyatMatrisi?.Satirlar != null)
            {
                foreach (var freshSatir in freshMatris.Satirlar)
                {
                    var userSatir = vm.FiyatMatrisi.Satirlar.FirstOrDefault(s => s.KiraciKategoriId == freshSatir.KiraciKategoriId);
                    if (userSatir != null)
                    {
                        foreach (var freshHucre in freshSatir.Hucreler)
                        {
                            var userHucre = userSatir.Hucreler.FirstOrDefault(h => h.BorcTipiId == freshHucre.BorcTipiId);
                            if (userHucre != null)
                            {
                                freshHucre.BirimDeger = userHucre.BirimDeger;
                                freshHucre.HesaplamaYontemi = userHucre.HesaplamaYontemi;
                                freshHucre.KdvOrani = userHucre.KdvOrani;
                            }
                        }
                    }
                }
            }
            vm.FiyatMatrisi = freshMatris;

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
