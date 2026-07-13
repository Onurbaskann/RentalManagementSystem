using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/Sector")]
public class AdminSectorController : Controller
{
    private const CategoryType Type = CategoryType.Sector;
    private readonly ICategoryRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminSectorController(ICategoryRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Sector.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListByTipiAsync(Type);
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.Sector.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraByTipiAsync(Type)) + 1;
        return View(new CategoryFormViewModel { Type = Type, Order = nextSira });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Sector.Create)]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        model.Type = Type;
        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Name);
        if (await _repo.KodExistsByTipiAsync(Type, kod))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new Category
        {
            Type = Type,
            Name = model.Name,
            Code = kod,
            Order = model.Order,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' sektörü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id}")]
    [Authorize(Policy = PermissionCatalog.Sector.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAndTipiAsync(id, Type);
        if (entity == null) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Sector.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] CategoryFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        model.Type = Type;
        if (!ModelState.IsValid) return View(model);

        var entity = await _repo.GetByIdAndTipiAsync(id, Type);
        if (entity == null) return NotFound();

        entity.Name = model.Name;
        entity.Order = model.Order;
        entity.IsActive = model.IsActive;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Sector.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAndTipiAsync(id, Type);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static CategoryFormViewModel ToFormVm(Category e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        Name = e.Name,
        Order = e.Order,
        IsActive = e.IsActive
    };
}
