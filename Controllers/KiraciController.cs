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
public class KiraciController : Controller
{
    private readonly IKiraciService _kiraciService;
    private readonly ISozlesmeService _sozlesmeService;
    private readonly IIstatistikService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;

    public KiraciController(
        IKiraciService kiraciService,
        ISozlesmeService sozlesmeService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx)
    {
        _kiraciService = kiraciService;
        _sozlesmeService = sozlesmeService;
        _istatistik = istatistik;
        _userManager = userManager;
        _ctx = ctx;
    }

    [Authorize(Policy = PermissionCatalog.Kiraci.View)]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole("Goruntuleyici") ? userId : null;
        var kiraciler = await _kiraciService.GetAllAsync(filterUserId);
        var sozlesmeler = await _sozlesmeService.GetAllAsync(userId: filterUserId);
        ViewBag.AktifSozlesme = sozlesmeler
            .Where(_istatistik.Aktif)
            .GroupBy(s => s.KiraciId)
            .ToDictionary(g => g.Key, g => g.Count());
        return View(kiraciler);
    }

    [Authorize(Policy = PermissionCatalog.Kiraci.View)]
    public async Task<IActionResult> Detay(int id)
    {
        string? scopedUserId = null;
        if (User.IsInRole("Goruntuleyici"))
        {
            scopedUserId = _userManager.GetUserId(User);
            var kiraciler = await _kiraciService.GetAllAsync(scopedUserId);
            if (!kiraciler.Any(k => k.Id == id)) return Forbid();
        }

        var k = await _kiraciService.GetByIdAsync(id);
        if (k == null) return NotFound();

        List<KiraSozlesmesi> sozlesmeler;
        if (scopedUserId != null)
        {
            var all = await _sozlesmeService.GetAllAsync(userId: scopedUserId);
            sozlesmeler = all.Where(s => s.KiraciId == id).ToList();
        }
        else
        {
            sozlesmeler = await _sozlesmeService.GetByKiraciIdAsync(id);
        }

        var vm = new KiraciDetayViewModel
        {
            Kiraci = k,
            Sozlesmeler = sozlesmeler
        };
        return View(vm);
    }

    private async Task PopulateKiraciViewBagAsync()
    {
        ViewBag.Kategoriler = await _ctx.KiraciKategorileri.Where(k => k.Aktif).OrderBy(k => k.Sira).ToListAsync();
        ViewBag.Sektorler = await _ctx.Sektorler.Where(s => s.Aktif).OrderBy(s => s.Sira).ToListAsync();
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Kiraci.Create)]
    public async Task<IActionResult> Ekle()
    {
        await PopulateKiraciViewBagAsync();
        var vm = new KiraciFormViewModel
        {
            KiraciNo = await _kiraciService.GenerateKiraciNoAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Kiraci.Create)]
    public async Task<IActionResult> Ekle(KiraciFormViewModel vm)
    {
        await ValidateKiraciAsync(vm);

        if (!ModelState.IsValid)
        {
            await PopulateKiraciViewBagAsync();
            return View(vm);
        }

        var k = BuildKiraciFromVm(vm);
        await _kiraciService.CreateAsync(k);
        TempData["Success"] = $"'{k.GosterimAdi}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = k.Id });
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Kiraci.Edit)]
    public async Task<IActionResult> Duzenle(int id)
    {
        var k = await _kiraciService.GetByIdAsync(id);
        if (k == null) return NotFound();

        await PopulateKiraciViewBagAsync();
        var vm = new KiraciFormViewModel
        {
            Id = k.Id,
            KiraciNo = k.KiraciNo,
            KiraciTuru = k.KiraciTuru,
            Ad = k.Ad,
            GercekAd = k.KiraciTuru == KiraciTuru.Gercek ? k.Ad : null,
            TuzelAd = k.KiraciTuru == KiraciTuru.Tuzel ? k.Ad : null,
            Soyad = k.Soyad,
            TcVatandasiDegil = !string.IsNullOrWhiteSpace(k.PasaportNo),
            TcKimlikNo = k.TcKimlikNo,
            PasaportNo = k.PasaportNo,
            Unvan = k.Unvan,
            AnneAdi = k.AnneAdi,
            BabaAdi = k.BabaAdi,
            DogumTarihi = k.DogumTarihi,
            DogumYeri = k.DogumYeri,
            TicaretSicilNo = k.TicaretSicilNo,
            VergiNo = k.VergiNo,
            VergiDairesi = k.VergiDairesi,
            MersisNo = k.MersisNo,
            Telefon = k.Telefon,
            Email = k.Email,
            Adres = k.Adres,
            KvkkOnayi = k.KvkkOnayi,
            KiraciKategoriId = k.KiraciKategoriId,
            SektorId = k.SektorId
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Kiraci.Edit)]
    public async Task<IActionResult> Duzenle(int id, KiraciFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        await ValidateKiraciAsync(vm, excludeId: id);

        if (!ModelState.IsValid)
        {
            await PopulateKiraciViewBagAsync();
            return View(vm);
        }

        var k = BuildKiraciFromVm(vm);
        k.Id = id;
        await _kiraciService.UpdateAsync(k);
        TempData["Success"] = "Kiracı bilgileri güncellendi.";
        return RedirectToAction("Detay", new { id });
    }

    private async Task ValidateKiraciAsync(KiraciFormViewModel vm, int? excludeId = null)
    {
        ModelState.Remove("Ad");
        ModelState.Remove("GercekAd");
        ModelState.Remove("TuzelAd");
        ModelState.Remove("Soyad");

        if (string.IsNullOrWhiteSpace(vm.KiraciNo))
        {
            ModelState.AddModelError("KiraciNo", "Kiracı No zorunludur.");
        }
        else if (await _kiraciService.KiraciNoExistsAsync(vm.KiraciNo, excludeId))
        {
            ModelState.AddModelError("KiraciNo", "Bu Kiracı No zaten kullanımda.");
        }

        if (vm.KiraciTuru == null)
            ModelState.AddModelError("KiraciTuru", "Kiracı türü seçilmelidir.");

        if (!vm.KiraciKategoriId.HasValue || vm.KiraciKategoriId <= 0)
            ModelState.AddModelError("KiraciKategoriId", "Kiracı kategorisi seçilmelidir.");

        if (!vm.SektorId.HasValue || vm.SektorId <= 0)
            ModelState.AddModelError("SektorId", "Sektör seçilmelidir.");

        if (vm.KiraciTuru == KiraciTuru.Gercek)
        {
            vm.Ad = vm.GercekAd;

            if (string.IsNullOrWhiteSpace(vm.GercekAd))
                ModelState.AddModelError("GercekAd", "Ad zorunludur.");
            if (string.IsNullOrWhiteSpace(vm.Soyad))
                ModelState.AddModelError("Soyad", "Soyad zorunludur.");
            if (!vm.DogumTarihi.HasValue)
                ModelState.AddModelError("DogumTarihi", "Doğum Tarihi zorunludur.");
            if (string.IsNullOrWhiteSpace(vm.AnneAdi))
                ModelState.AddModelError("AnneAdi", "Anne Adı zorunludur.");
            if (string.IsNullOrWhiteSpace(vm.BabaAdi))
                ModelState.AddModelError("BabaAdi", "Baba Adı zorunludur.");

            if (vm.TcVatandasiDegil)
            {
                if (string.IsNullOrWhiteSpace(vm.PasaportNo))
                    ModelState.AddModelError("PasaportNo", "Pasaport No zorunludur.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vm.TcKimlikNo))
                    ModelState.AddModelError("TcKimlikNo", "TC Kimlik No zorunludur.");
                else if (vm.TcKimlikNo.Length != 11 || !vm.TcKimlikNo.All(char.IsDigit))
                    ModelState.AddModelError("TcKimlikNo", "TC Kimlik No 11 haneli rakamdan oluşmalıdır.");
            }
        }
        else if (vm.KiraciTuru == KiraciTuru.Tuzel)
        {
            vm.Ad = vm.TuzelAd;

            if (string.IsNullOrWhiteSpace(vm.TuzelAd))
                ModelState.AddModelError("TuzelAd", "Firma / Kurum Adı zorunludur.");

            if (string.IsNullOrWhiteSpace(vm.VergiNo))
                ModelState.AddModelError("VergiNo", "Vergi No zorunludur.");
            else if (vm.VergiNo.Length != 10 || !vm.VergiNo.All(char.IsDigit))
                ModelState.AddModelError("VergiNo", "Vergi No 10 haneli rakamdan oluşmalıdır.");

            if (string.IsNullOrWhiteSpace(vm.VergiDairesi))
                ModelState.AddModelError("VergiDairesi", "Vergi Dairesi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(vm.Telefon))
            ModelState.AddModelError("Telefon", "Telefon zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Email))
            ModelState.AddModelError("Email", "E-posta zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Adres))
            ModelState.AddModelError("Adres", "Adres zorunludur.");

        if (!vm.KvkkOnayi)
            ModelState.AddModelError("KvkkOnayi", "KVKK aydınlatma metni onayı zorunludur.");
    }

    private static Kiraci BuildKiraciFromVm(KiraciFormViewModel vm)
    {
        var tur = vm.KiraciTuru ?? KiraciTuru.Gercek;
        var k = new Kiraci
        {
            KiraciNo = vm.KiraciNo,
            KiraciTuru = tur,
            Ad = vm.Ad,
            Telefon = vm.Telefon,
            Email = vm.Email,
            Adres = vm.Adres,
            KvkkOnayi = vm.KvkkOnayi,
            KiraciKategoriId = vm.KiraciKategoriId,
            SektorId = vm.SektorId
        };

        if (tur == KiraciTuru.Gercek)
        {
            k.Soyad = vm.Soyad;
            k.TcKimlikNo = vm.TcVatandasiDegil ? null : vm.TcKimlikNo;
            k.PasaportNo = vm.TcVatandasiDegil ? vm.PasaportNo : null;
            k.Unvan = vm.Unvan;
            k.AnneAdi = vm.AnneAdi;
            k.BabaAdi = vm.BabaAdi;
            k.DogumTarihi = vm.DogumTarihi;
            k.DogumYeri = vm.DogumYeri;
            k.TicaretSicilNo = null;
            k.VergiNo = null;
            k.VergiDairesi = null;
            k.MersisNo = null;
        }
        else
        {
            k.TicaretSicilNo = vm.TicaretSicilNo;
            k.VergiNo = vm.VergiNo;
            k.VergiDairesi = vm.VergiDairesi;
            k.MersisNo = vm.MersisNo;
            k.Soyad = null;
            k.TcKimlikNo = null;
            k.PasaportNo = null;
            k.Unvan = null;
            k.AnneAdi = null;
            k.BabaAdi = null;
            k.DogumTarihi = null;
            k.DogumYeri = null;
        }

        return k;
    }
}
