using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.BelgeTuruPerm.Manage)]
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
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
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
    public async Task<IActionResult> Create(BelgeTuruFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kod = model.Kod.Trim().ToUpperInvariant();
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
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
            IsActive = model.IsActive
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' belge türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BelgeTuruFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        var kod = model.Kod.Trim().ToUpperInvariant();
        if (await _repo.KodExistsAsync(kod, id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        entity.Kod = kod;
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
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Sil/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        entity.IsDeleted = true;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' silindi.";
        return RedirectToAction(nameof(Index));
    }

    private static BelgeTuruFormViewModel ToFormVm(BelgeTuru e) => new()
    {
        Id = e.Id,
        Kod = e.Kod,
        Ad = e.Ad,
        Aciklama = e.Aciklama,
        HedefEntite = e.HedefEntite,
        Zorunlu = e.Zorunlu,
        IzinVerilenUzantilar = e.IzinVerilenUzantilar,
        MaxBoyutMb = e.MaxBoyutMb,
        Sira = e.Sira,
        IsActive = e.IsActive
    };
}
