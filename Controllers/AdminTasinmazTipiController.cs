using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.TasinmazTipi.Manage)]
[Route("Admin/TasinmazTipi")]
public class AdminTasinmazTipiController : Controller
{
    private readonly ITasinmazTipiRepository _repo;
    private readonly IUnitOfWork _uow;

    public AdminTasinmazTipiController(ITasinmazTipiRepository repo, IUnitOfWork uow)
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
        var nextSira = (await _repo.GetMaxSiraAsync()) + 1;
        return View(new TasinmazTipiFormViewModel { Sira = nextSira, TekParcaDestekli = true });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TasinmazTipiFormViewModel model)
    {
        if (!model.TekParcaDestekli && !model.BirimBazliDestekli)
            ModelState.AddModelError("kiralamaSekli", "En az bir kiralama şekli seçilmelidir.");

        if (!ModelState.IsValid) return View(model);

        var kod = CodeSlugger.ToCode(model.Ad);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            return View(model);
        }

        var entity = new TasinmazTipi
        {
            Ad = model.Ad,
            Kod = kod,
            Sira = model.Sira,
            Aktif = model.Aktif,
            OlusturmaTarihi = DateTime.UtcNow,
            TekParcaDestekli = model.TekParcaDestekli,
            BirimBazliDestekli = model.BirimBazliDestekli
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' taşınmaz tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TasinmazTipiFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!model.TekParcaDestekli && !model.BirimBazliDestekli)
            ModelState.AddModelError("kiralamaSekli", "En az bir kiralama şekli seçilmelidir.");

        if (!ModelState.IsValid) return View(model);

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        entity.Ad = model.Ad;
        entity.Sira = model.Sira;
        entity.Aktif = model.Aktif;
        entity.TekParcaDestekli = model.TekParcaDestekli;
        entity.BirimBazliDestekli = model.BirimBazliDestekli;

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
        entity.Aktif = !entity.Aktif;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static TasinmazTipiFormViewModel ToFormVm(TasinmazTipi e) => new()
    {
        Id = e.Id,
        Ad = e.Ad,
        Sira = e.Sira,
        Aktif = e.Aktif,
        TekParcaDestekli = e.TekParcaDestekli,
        BirimBazliDestekli = e.BirimBazliDestekli
    };
}
