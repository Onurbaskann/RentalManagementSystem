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

    private IQueryable<Kategori> Query() =>
        _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tasinmaz);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await Query().OrderBy(t => t.Sira).ThenBy(t => t.Ad).ToListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var nextSira = (Query().Max(t => (int?)t.Sira) ?? 0) + 1;
        return View(new Kategori { Tipi = KategoriTipi.Tasinmaz, Sira = nextSira, OlusturmaTarihi = DateTime.UtcNow, TekParcaDestekli = true });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Kategori model, bool tekParca, bool birimBazli)
    {
        if (string.IsNullOrWhiteSpace(model.Ad))
            ModelState.AddModelError(nameof(model.Ad), "Ad zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Kod))
            ModelState.AddModelError(nameof(model.Kod), "Kod zorunludur.");
        if (!tekParca && !birimBazli)
            ModelState.AddModelError("kiralamaSekli", "En az bir kiralama şekli seçilmelidir.");

        if (!ModelState.IsValid)
            return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await Query().AnyAsync(t => t.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.Tipi = KategoriTipi.Tasinmaz;
        model.OlusturmaTarihi = DateTime.UtcNow;
        model.TekParcaDestekli = tekParca;
        model.BirimBazliDestekli = birimBazli;

        _ctx.Kategoriler.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' taşınmaz tipi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Query().FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Kategori model, bool tekParca, bool birimBazli)
    {
        if (id != model.Id) return BadRequest();

        if (string.IsNullOrWhiteSpace(model.Ad))
            ModelState.AddModelError(nameof(model.Ad), "Ad zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Kod))
            ModelState.AddModelError(nameof(model.Kod), "Kod zorunludur.");
        if (!tekParca && !birimBazli)
            ModelState.AddModelError("kiralamaSekli", "En az bir kiralama şekli seçilmelidir.");

        if (!ModelState.IsValid)
            return View(model);

        model.Kod = model.Kod.Trim().ToUpper();
        if (await Query().AnyAsync(t => t.Kod == model.Kod && t.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            return View(model);
        }

        model.Tipi = KategoriTipi.Tasinmaz;
        model.TekParcaDestekli = tekParca;
        model.BirimBazliDestekli = birimBazli;
        _ctx.Kategoriler.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{model.Ad}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await Query().FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}
