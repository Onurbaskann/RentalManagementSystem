using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.BorcTipi.Manage)]
[Route("Admin/BorcTipi")]
public class AdminBorcTipiController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public AdminBorcTipiController(ApplicationDbContext ctx) => _ctx = ctx;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _ctx.BorcTipleri.OrderBy(b => b.Sira).ThenBy(b => b.Ad).ToListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var nextSira = (_ctx.BorcTipleri.Max(b => (int?)b.Sira) ?? 0) + 1;
        return View(new BorcTipi { Sira = nextSira });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BorcTipi model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.BorcTipleri.AnyAsync(b => b.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        _ctx.BorcTipleri.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' borç tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _ctx.BorcTipleri.FindAsync(id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BorcTipi model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await _ctx.BorcTipleri.FindAsync(id);
        if (entity == null) return NotFound();

        // Sistem tiplerinde Kod değiştirilemez — form değeri yok sayılır
        if (entity.Sistem)
            model.Kod = entity.Kod;

        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await _ctx.BorcTipleri.AnyAsync(b => b.Kod == model.Kod && b.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        entity.Ad = model.Ad;
        entity.Kod = model.Kod;
        entity.Davranis = model.Davranis;
        entity.Sira = model.Sira;
        entity.Aktif = model.Aktif;
        // entity.Sistem hiç değiştirilmez

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _ctx.BorcTipleri.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Sistem && entity.Aktif)
        {
            TempData["Error"] = $"'{entity.Ad}' bir sistem kaydıdır ve pasif yapılamaz.";
            return RedirectToAction(nameof(Index));
        }

        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SiraDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SiraDegistir(int id, int yeniSira)
    {
        var entity = await _ctx.BorcTipleri.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Sira = yeniSira;
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
