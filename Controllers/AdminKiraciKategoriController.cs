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
[Route("Admin/KiraciKategori")]
public class AdminKiraciKategoriController : Controller
{
    private const KategoriTipi Tipi = KategoriTipi.Tenant;
    private readonly IKategoriRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminKiraciKategoriController(IKategoriRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListByTipiAsync(Tipi);
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraByTipiAsync(Tipi)) + 1;
        return View(new KategoriFormViewModel { Tipi = Tipi, Sira = nextSira });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Create)]
    public async Task<IActionResult> Create(KategoriFormViewModel model)
    {
        model.Tipi = Tipi;
        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsByTipiAsync(Tipi, kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new Kategori
        {
            Tipi = Tipi,
            Ad = model.Ad,
            Kod = kod,
            Sira = model.Sira,
            IsActive = model.Aktif,
            OlusturmaTarihi = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' kiracı kategorisi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAndTipiAsync(id, Tipi);
        if (entity == null) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Edit)]
    public async Task<IActionResult> Edit(int id, KategoriFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        model.Tipi = Tipi;
        if (!ModelState.IsValid) return View(model);

        var entity = await _repo.GetByIdAndTipiAsync(id, Tipi);
        if (entity == null) return NotFound();

        entity.Ad = model.Ad;
        entity.Sira = model.Sira;
        entity.IsActive = model.Aktif;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAndTipiAsync(id, Tipi);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static KategoriFormViewModel ToFormVm(Kategori e) => new()
    {
        Id = e.Id,
        Tipi = e.Tipi,
        Ad = e.Ad,
        Sira = e.Sira,
        Aktif = e.IsActive
    };
}
