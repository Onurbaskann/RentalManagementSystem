using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.TasinmazTipiPerm.Manage)]
[Route("Admin/TasinmazTipi")]
public class AdminTasinmazTipiController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public AdminTasinmazTipiController(ApplicationDbContext ctx) => _ctx = ctx;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _ctx.TasinmazTipleri.OrderBy(t => t.Sira).ThenBy(t => t.Ad).ToListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var nextSira = (_ctx.TasinmazTipleri.Max(t => (int?)t.Sira) ?? 0) + 1;
        return View(new TasinmazTipi { Sira = nextSira, OlusturmaTarihi = DateTime.UtcNow });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TasinmazTipi model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.TasinmazTipleri.AnyAsync(t => t.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.OlusturmaTarihi = DateTime.UtcNow;
        _ctx.TasinmazTipleri.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' taşınmaz tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _ctx.TasinmazTipleri.FindAsync(id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TasinmazTipi model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.TasinmazTipleri.AnyAsync(t => t.Kod == model.Kod && t.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        _ctx.TasinmazTipleri.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _ctx.TasinmazTipleri.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}
