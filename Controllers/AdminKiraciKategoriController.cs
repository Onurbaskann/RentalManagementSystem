using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.KiraciKategoriPerm.Manage)]
[Route("Admin/KiraciKategori")]
public class AdminKiraciKategoriController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public AdminKiraciKategoriController(ApplicationDbContext ctx) => _ctx = ctx;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _ctx.KiraciKategorileri.OrderBy(k => k.Sira).ThenBy(k => k.Ad).ToListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var nextSira = (_ctx.KiraciKategorileri.Max(k => (int?)k.Sira) ?? 0) + 1;
        return View(new KiraciKategori { Sira = nextSira, OlusturmaTarihi = DateTime.UtcNow });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KiraciKategori model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.KiraciKategorileri.AnyAsync(k => k.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.OlusturmaTarihi = DateTime.UtcNow;
        _ctx.KiraciKategorileri.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' kiracı kategorisi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _ctx.KiraciKategorileri.FindAsync(id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KiraciKategori model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.KiraciKategorileri.AnyAsync(k => k.Kod == model.Kod && k.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        _ctx.KiraciKategorileri.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _ctx.KiraciKategorileri.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}
