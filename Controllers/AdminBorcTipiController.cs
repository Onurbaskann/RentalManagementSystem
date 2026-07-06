using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/ChargeType")]
public class AdminBorcTipiController : Controller
{
    private readonly IBorcTipiRepository _repo;
    private readonly IUnitTypeRepository _birimTuruRepo;
    private readonly IUnitOfWork _uow;

    public AdminBorcTipiController(
        IBorcTipiRepository repo,
        IUnitTypeRepository birimTuruRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _birimTuruRepo = birimTuruRepo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.ChargeType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.ChargeType.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraAsync()) + 1;
        return View(new BorcTipiFormViewModel { Sira = nextSira });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Create)]
    public async Task<IActionResult> Create(BorcTipiFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new ChargeType
        {
            Name = model.Ad,
            Code = kod,
            Behavior = model.Davranis,
            SortOrder = model.Sira,
            IsActive = model.Aktif,
            IsSystem = false
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' borç tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.ChargeType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = ToFormVm(entity);
        return View(vm);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Edit)]
    public async Task<IActionResult> Edit(int id, BorcTipiFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        // Sistem tiplerinde Davranış değiştirilemez
        if (entity.IsSystem)
            model.Davranis = entity.Behavior;

        if (!ModelState.IsValid)
        {
            model.Sistem = entity.IsSystem;
            return View(model);
        }

        entity.Name = model.Ad;
        entity.Behavior = model.Davranis;
        entity.SortOrder = model.Sira;
        entity.IsActive = model.Aktif;
        // entity.Kod ve entity.Sistem hiç değiştirilmez

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        if (entity.IsActive)
        {
            if (entity.IsSystem)
            {
                TempData["Error"] = $"'{entity.Name}' bir sistem kaydıdır ve pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (await _birimTuruRepo.AnyAktifByBorcTipiIdAsync(id))
            {
                TempData["Error"] = "Bu borç tipi aktif bir birim türüne bağlı. Önce ilgili birim türünü pasif yapın.";
                return RedirectToAction(nameof(Index));
            }
        }

        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SiraDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Edit)]
    public async Task<IActionResult> SiraDegistir(int id, int yeniSira)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        entity.SortOrder = yeniSira;
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static BorcTipiFormViewModel ToFormVm(ChargeType e) => new()
    {
        Id = e.Id,
        Ad = e.Name,
        Davranis = e.Behavior,
        Sira = e.SortOrder,
        Aktif = e.IsActive,
        Sistem = e.IsSystem
    };
}
