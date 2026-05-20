using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.SektorPerm.Manage)]
[Route("Admin/Sektor")]
public class AdminSektorController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public AdminSektorController(ApplicationDbContext ctx) => _ctx = ctx;

    private IQueryable<Kategori> Query() =>
        _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await Query().OrderBy(s => s.Sira).ThenBy(s => s.Ad).ToListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var nextSira = (Query().Max(s => (int?)s.Sira) ?? 0) + 1;
        return View(new Kategori { Tipi = KategoriTipi.Sektor, Sira = nextSira, OlusturmaTarihi = DateTime.UtcNow });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Kategori model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await Query().AnyAsync(s => s.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.Tipi = KategoriTipi.Sektor;
        model.OlusturmaTarihi = DateTime.UtcNow;
        _ctx.Kategoriler.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' sektörü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Query().FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Kategori model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await Query().AnyAsync(s => s.Kod == model.Kod && s.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.Tipi = KategoriTipi.Sektor;
        _ctx.Kategoriler.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await Query().FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}
