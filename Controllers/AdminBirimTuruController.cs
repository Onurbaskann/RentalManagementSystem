using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/BirimTuru")]
public class AdminBirimTuruController : Controller
{
    private readonly IBirimTuruRepository _repo;
    private readonly IBorcTipiRepository _borcTipiRepo;
    private readonly IUnitOfWork _uow;

    public AdminBirimTuruController(
        IBirimTuruRepository repo,
        IBorcTipiRepository borcTipiRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _borcTipiRepo = borcTipiRepo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.BirimTuru.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.BirimTuru.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraAsync()) + 1;
        var vm = new BirimTuruFormViewModel
        {
            Sira = nextSira,
            KiralanabilirMi = true,
            BorcTipiAdaylari = await _borcTipiRepo.GetRezervasyonAdaylariAsync()
        };
        return View(vm);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.BirimTuru.Create)]
    public async Task<IActionResult> Create(BirimTuruFormViewModel model)
    {
        if (model.RezervasyonYapilabilirMi && (!model.BorcTipiId.HasValue || model.BorcTipiId <= 0))
            ModelState.AddModelError(nameof(model.BorcTipiId), "Rezervasyon birim türü için borç tipi seçilmelidir.");

        if (model.KiralanabilirMi == model.RezervasyonYapilabilirMi)
            ModelState.AddModelError(string.Empty,
                "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Rezervasyon yapılabilir.");

        if (!ModelState.IsValid)
        {
            model.BorcTipiAdaylari = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
            return View(model);
        }

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            model.BorcTipiAdaylari = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
            return View(model);
        }

        var entity = new BirimTuru
        {
            Ad = model.Ad,
            Kod = kod,
            Sira = model.Sira,
            KiralanabilirMi = model.KiralanabilirMi,
            RezervasyonYapilabilirMi = model.RezervasyonYapilabilirMi,
            BorcTipiId = model.KiralanabilirMi ? null : model.BorcTipiId,
            Aktif = model.Aktif,
            OlusturmaTarihi = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' birim türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.BirimTuru.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = ToFormVm(entity);
        vm.BorcTipiAdaylari = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
        return View(vm);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.BirimTuru.Edit)]
    public async Task<IActionResult> Edit(int id, BirimTuruFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (model.RezervasyonYapilabilirMi && (!model.BorcTipiId.HasValue || model.BorcTipiId <= 0))
            ModelState.AddModelError(nameof(model.BorcTipiId), "Rezervasyon birim türü için borç tipi seçilmelidir.");

        if (model.KiralanabilirMi == model.RezervasyonYapilabilirMi)
            ModelState.AddModelError(string.Empty,
                "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Rezervasyon yapılabilir.");

        if (!ModelState.IsValid)
        {
            model.BorcTipiAdaylari = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
            return View(model);
        }

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        entity.Ad = model.Ad;
        entity.Sira = model.Sira;
        entity.KiralanabilirMi = model.KiralanabilirMi;
        entity.RezervasyonYapilabilirMi = model.RezervasyonYapilabilirMi;
        entity.BorcTipiId = model.KiralanabilirMi ? null : model.BorcTipiId;
        entity.Aktif = model.Aktif;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.BirimTuru.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        if (entity.Aktif) // Pasife çekme
        {
            if (await _repo.HasAktifTahakkukForBirimTuruAsync(id))
            {
                TempData["Error"] = "Bu birim türüne bağlı birimlerde aktif tahakkuk bulunduğu için pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (await _repo.HasPlanlanmisRezervasyonForBirimTuruAsync(id))
            {
                TempData["Error"] = "Bu birim türüne bağlı birimlerde planlanmış rezervasyon bulunduğu için pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            // Cascade BorcTipi pasif (başka aktif BirimTuru kullanmıyorsa)
            if (entity.BorcTipiId.HasValue)
            {
                var baskaKullananVar = await _repo.AnyAktifByBorcTipiIdAsync(entity.BorcTipiId.Value, id);
                if (!baskaKullananVar)
                {
                    var borcTipi = await _borcTipiRepo.GetByIdAsync(entity.BorcTipiId.Value);
                    if (borcTipi != null) borcTipi.Aktif = false;
                }
            }
        }

        entity.Aktif = !entity.Aktif;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static BirimTuruFormViewModel ToFormVm(BirimTuru e) => new()
    {
        Id = e.Id,
        Ad = e.Ad,
        Sira = e.Sira,
        KiralanabilirMi = e.KiralanabilirMi,
        RezervasyonYapilabilirMi = e.RezervasyonYapilabilirMi,
        BorcTipiId = e.BorcTipiId,
        Aktif = e.Aktif
    };
}
