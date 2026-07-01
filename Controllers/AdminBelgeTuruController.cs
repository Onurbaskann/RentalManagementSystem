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
[Route("Admin/BelgeTuru")]
public class AdminBelgeTuruController : Controller
{
    private readonly IBelgeTuruRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminBelgeTuruController(IBelgeTuruRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Module)]
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
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Create)]
    public async Task<IActionResult> Create(BelgeTuruFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new BelgeTuru
        {
            Kod = kod,
            Ad = model.Ad.Trim(),
            Aciklama = model.Aciklama?.Trim(),
            HedefEntite = model.HedefEntite,
            Zorunlu = model.Zorunlu,
            IzinVerilenUzantilar = model.IzinVerilenUzantilar.Trim().ToLowerInvariant(),
            MaxBoyutMb = model.MaxBoyutMb,
            Sira = model.Sira,
            IsActive = model.IsActive,
            Sistem = false
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' belge türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Edit)]
    public async Task<IActionResult> Edit(int id, BelgeTuruFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        // Sistem tiplerinde HedefEntite değiştirilemez
        if (entity.Sistem)
            model.HedefEntite = entity.HedefEntite;

        if (!ModelState.IsValid)
        {
            model.Sistem = entity.Sistem;
            return View(model);
        }

        entity.Ad = model.Ad.Trim();
        entity.Aciklama = model.Aciklama?.Trim();
        entity.HedefEntite = model.HedefEntite;
        entity.Zorunlu = model.Zorunlu;
        entity.IzinVerilenUzantilar = model.IzinVerilenUzantilar.Trim().ToLowerInvariant();
        entity.MaxBoyutMb = model.MaxBoyutMb;
        entity.Sira = model.Sira;
        entity.IsActive = model.IsActive;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        if (entity.IsActive && entity.Sistem)
        {
            TempData["Error"] = $"'{entity.Ad}' bir sistem kaydıdır ve pasif yapılamaz.";
            return RedirectToAction(nameof(Index));
        }

        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Sil/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.BelgeTuru.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        if (entity.Sistem)
        {
            TempData["Error"] = $"'{entity.Ad}' bir sistem kaydıdır ve silinemez.";
            return RedirectToAction(nameof(Index));
        }

        entity.IsDeleted = true;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' silindi.";
        return RedirectToAction(nameof(Index));
    }

    private static BelgeTuruFormViewModel ToFormVm(BelgeTuru e) => new()
    {
        Id = e.Id,
        Ad = e.Ad,
        Aciklama = e.Aciklama,
        HedefEntite = e.HedefEntite,
        Zorunlu = e.Zorunlu,
        IzinVerilenUzantilar = e.IzinVerilenUzantilar,
        MaxBoyutMb = e.MaxBoyutMb,
        Sira = e.Sira,
        IsActive = e.IsActive,
        Sistem = e.Sistem
    };
}
