using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.BirimTuruPerm.Manage)]
[Route("Admin/BirimTuru")]
public class AdminBirimTuruController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public AdminBirimTuruController(ApplicationDbContext ctx) => _ctx = ctx;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _ctx.BirimTurleri.OrderBy(b => b.Sira).ThenBy(b => b.Ad).ToListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var nextSira = (_ctx.BirimTurleri.Max(b => (int?)b.Sira) ?? 0) + 1;
        return View(new BirimTuru { Sira = nextSira, OlusturmaTarihi = DateTime.UtcNow });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BirimTuru model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.BirimTurleri.AnyAsync(b => b.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.OlusturmaTarihi = DateTime.UtcNow;
        _ctx.BirimTurleri.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' birim türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _ctx.BirimTurleri.FindAsync(id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BirimTuru model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.BirimTurleri.AnyAsync(b => b.Kod == model.Kod && b.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        _ctx.BirimTurleri.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _ctx.BirimTurleri.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}
