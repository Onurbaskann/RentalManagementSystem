using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.SektorPerm.Manage)]
[Route("Admin/Sektor")]
public class AdminSektorController : Controller
{
    private const KategoriTipi Tipi = KategoriTipi.Sektor;
    private readonly IKategoriRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminSektorController(IKategoriRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListByTipiAsync(Tipi);
        return View(list);
    }

    [HttpGet("Ekle")]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraByTipiAsync(Tipi)) + 1;
        return View(new KategoriFormViewModel { Tipi = Tipi, Sira = nextSira });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KategoriFormViewModel model)
    {
        model.Tipi = Tipi;
        if (!ModelState.IsValid) return View(model);

        var kod = model.Kod.Trim().ToUpper();
        if (await _repo.KodExistsByTipiAsync(Tipi, kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        var entity = new Kategori
        {
            Tipi = Tipi,
            Ad = model.Ad,
            Kod = kod,
            Sira = model.Sira,
            Aktif = model.Aktif,
            OlusturmaTarihi = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' sektörü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAndTipiAsync(id, Tipi);
        if (entity == null) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KategoriFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        model.Tipi = Tipi;
        if (!ModelState.IsValid) return View(model);

        var entity = await _repo.GetByIdAndTipiAsync(id, Tipi);
        if (entity == null) return NotFound();

        var kod = model.Kod.Trim().ToUpper();
        if (await _repo.KodExistsByTipiAsync(Tipi, kod, id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        entity.Ad = model.Ad;
        entity.Kod = kod;
        entity.Sira = model.Sira;
        entity.Aktif = model.Aktif;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAndTipiAsync(id, Tipi);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static KategoriFormViewModel ToFormVm(Kategori e) => new()
    {
        Id = e.Id,
        Tipi = e.Tipi,
        Ad = e.Ad,
        Kod = e.Kod,
        Sira = e.Sira,
        Aktif = e.Aktif
    };
}
