using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.BorcTipi.Manage)]
[Route("Admin/BorcTipi")]
public class AdminBorcTipiController : Controller
{
    private readonly IBorcTipiRepository _repo;
    private readonly IBirimTuruRepository _birimTuruRepo;
    private readonly IUnitOfWork _uow;

    public AdminBorcTipiController(
        IBorcTipiRepository repo,
        IBirimTuruRepository birimTuruRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _birimTuruRepo = birimTuruRepo;
        _uow = uow;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraAsync()) + 1;
        return View(new BorcTipiFormViewModel { Sira = nextSira });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BorcTipiFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new BorcTipi
        {
            Ad = model.Ad,
            Kod = kod,
            Davranis = model.Davranis,
            Sira = model.Sira,
            Aktif = model.Aktif,
            Sistem = false
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' borç tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = ToFormVm(entity);
        return View(vm);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BorcTipiFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        // Sistem tiplerinde Davranış değiştirilemez
        if (entity.Sistem)
            model.Davranis = entity.Davranis;

        if (!ModelState.IsValid)
        {
            model.Sistem = entity.Sistem;
            return View(model);
        }

        entity.Ad = model.Ad;
        entity.Davranis = model.Davranis;
        entity.Sira = model.Sira;
        entity.Aktif = model.Aktif;
        // entity.Kod ve entity.Sistem hiç değiştirilmez

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        if (entity.Aktif)
        {
            if (entity.Sistem)
            {
                TempData["Error"] = $"'{entity.Ad}' bir sistem kaydıdır ve pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (await _birimTuruRepo.AnyAktifByBorcTipiIdAsync(id))
            {
                TempData["Error"] = "Bu borç tipi aktif bir birim türüne bağlı. Önce ilgili birim türünü pasif yapın.";
                return RedirectToAction(nameof(Index));
            }
        }

        entity.Aktif = !entity.Aktif;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SiraDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SiraDegistir(int id, int yeniSira)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        entity.Sira = yeniSira;
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static BorcTipiFormViewModel ToFormVm(BorcTipi e) => new()
    {
        Id = e.Id,
        Ad = e.Ad,
        Davranis = e.Davranis,
        Sira = e.Sira,
        Aktif = e.Aktif,
        Sistem = e.Sistem
    };
}
