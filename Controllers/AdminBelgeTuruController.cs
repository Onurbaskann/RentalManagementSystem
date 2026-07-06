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
[Route("Admin/DocumentType")]
public class AdminBelgeTuruController : Controller
{
    private readonly IDocumentTypeRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminBelgeTuruController(IDocumentTypeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.DocumentType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.DocumentType.Module)]
    public async Task<IActionResult> Create()
    {
        var vm = new BelgeTuruFormViewModel
        {
            Sira = (await _repo.GetMaxSiraAsync()) + 1
        };
        return View(vm);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Create)]
    public async Task<IActionResult> Create(BelgeTuruFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new DocumentType
        {
            Code = kod,
            Name = model.Ad.Trim(),
            Description = model.Aciklama?.Trim(),
            TargetEntity = model.HedefEntite,
            Required = model.Zorunlu,
            AllowedExtensions = model.IzinVerilenUzantilar.Trim().ToLowerInvariant(),
            MaxSizeMb = model.MaxBoyutMb,
            SortOrder = model.Sira,
            IsActive = model.IsActive,
            IsSystem = false
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' belge türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.DocumentType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Edit)]
    public async Task<IActionResult> Edit(int id, BelgeTuruFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        // Sistem tiplerinde HedefEntite değiştirilemez
        if (entity.IsSystem)
            model.HedefEntite = entity.TargetEntity;

        if (!ModelState.IsValid)
        {
            model.Sistem = entity.IsSystem;
            return View(model);
        }

        entity.Name = model.Ad.Trim();
        entity.Description = model.Aciklama?.Trim();
        entity.TargetEntity = model.HedefEntite;
        entity.Required = model.Zorunlu;
        entity.AllowedExtensions = model.IzinVerilenUzantilar.Trim().ToLowerInvariant();
        entity.MaxSizeMb = model.MaxBoyutMb;
        entity.SortOrder = model.Sira;
        entity.IsActive = model.IsActive;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        if (entity.IsActive && entity.IsSystem)
        {
            TempData["Error"] = $"'{entity.Name}' bir sistem kaydıdır ve pasif yapılamaz.";
            return RedirectToAction(nameof(Index));
        }

        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Sil/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        if (entity.IsSystem)
        {
            TempData["Error"] = $"'{entity.Name}' bir sistem kaydıdır ve silinemez.";
            return RedirectToAction(nameof(Index));
        }

        entity.IsDeleted = true;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' silindi.";
        return RedirectToAction(nameof(Index));
    }

    private static BelgeTuruFormViewModel ToFormVm(DocumentType e) => new()
    {
        Id = e.Id,
        Ad = e.Name,
        Aciklama = e.Description,
        HedefEntite = e.TargetEntity,
        Zorunlu = e.Required,
        IzinVerilenUzantilar = e.AllowedExtensions,
        MaxBoyutMb = e.MaxSizeMb,
        Sira = e.SortOrder,
        IsActive = e.IsActive,
        Sistem = e.IsSystem
    };
}
