using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/TasinmazTipi")]
public class AdminPropertyTypeController : Controller
{
    private readonly ITasinmazTipiRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminPropertyTypeController(ITasinmazTipiRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.PropertyType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.PropertyType.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraAsync()) + 1;
        return View(new TasinmazTipiFormViewModel { Sira = nextSira, TekBirimDestekli = true });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PropertyType.Create)]
    public async Task<IActionResult> Create(TasinmazTipiFormViewModel model)
    {
        if (!model.TekBirimDestekli && !model.CokluBirimDestekli)
            ModelState.AddModelError("birimYapisi", "En az bir birim yapısı seçilmelidir.");

        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new PropertyType
        {
            Name = model.Ad,
            Code = kod,
            SortOrder = model.Sira,
            IsActive = model.Aktif,
            SupportsSingleUnit = model.TekBirimDestekli,
            SupportsMultipleUnits = model.CokluBirimDestekli
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' taşınmaz tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id}")]
    [Authorize(Policy = PermissionCatalog.PropertyType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PropertyType.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] TasinmazTipiFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!model.TekBirimDestekli && !model.CokluBirimDestekli)
            ModelState.AddModelError("birimYapisi", "En az bir birim yapısı seçilmelidir.");

        if (!ModelState.IsValid) return View(model);

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        entity.Name = model.Ad;
        entity.SortOrder = model.Sira;
        entity.IsActive = model.Aktif;
        entity.SupportsSingleUnit = model.TekBirimDestekli;
        entity.SupportsMultipleUnits = model.CokluBirimDestekli;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PropertyType.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static TasinmazTipiFormViewModel ToFormVm(PropertyType e) => new()
    {
        Id = e.Id,
        Ad = e.Name,
        Sira = e.SortOrder,
        Aktif = e.IsActive,
        TekBirimDestekli = e.SupportsSingleUnit,
        CokluBirimDestekli = e.SupportsMultipleUnits
    };
}
