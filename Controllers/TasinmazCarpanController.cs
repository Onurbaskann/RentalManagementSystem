using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Tasinmaz")]
public class TasinmazCarpanController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public TasinmazCarpanController(ApplicationDbContext ctx) => _ctx = ctx;

    private async Task PopulateViewBagAsync()
    {
        ViewBag.KiraciKategorileri = await _ctx.KiraciKategorileri
            .Where(k => k.Aktif)
            .OrderBy(k => k.Sira)
            .ToListAsync();
    }

    [HttpGet("{tasinmazId:int}/Carpanlar/Ekle")]
    [Authorize(Policy = PermissionCatalog.TasinmazCarpanPerm.Manage)]
    public async Task<IActionResult> Create(int tasinmazId)
    {
        var tasinmaz = await _ctx.Tasinmazlar.FindAsync(tasinmazId);
        if (tasinmaz == null) return NotFound();

        await PopulateViewBagAsync();
        ViewBag.TasinmazId = tasinmazId;
        ViewBag.TasinmazAd = tasinmaz.Ad;

        return View(new TasinmazKategoriCarpan
        {
            TasinmazId = tasinmazId,
            Aktif = true,
            OlusturmaTarihi = DateTime.UtcNow
        });
    }

    [HttpPost("{tasinmazId:int}/Carpanlar/Ekle")]
    [Authorize(Policy = PermissionCatalog.TasinmazCarpanPerm.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int tasinmazId, TasinmazKategoriCarpan model)
    {
        model.TasinmazId = tasinmazId;
        model.OlusturmaTarihi = DateTime.UtcNow;
        ModelState.Remove(nameof(model.Tasinmaz));
        ModelState.Remove(nameof(model.KiraciKategori));

        if (model.Carpan <= 0)
            ModelState.AddModelError(nameof(model.Carpan), "Çarpan 0'dan büyük olmalıdır.");

        if (ModelState.IsValid)
        {
            var mevcutVar = await _ctx.TasinmazKategoriCarpanlari
                .AnyAsync(c => c.TasinmazId == tasinmazId && c.KiraciKategoriId == model.KiraciKategoriId);
            if (mevcutVar)
                ModelState.AddModelError(nameof(model.KiraciKategoriId), "Bu taşınmaz için seçili kategoride zaten bir çarpan tanımlı.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();
            ViewBag.TasinmazId = tasinmazId;
            var t = await _ctx.Tasinmazlar.FindAsync(tasinmazId);
            ViewBag.TasinmazAd = t?.Ad;
            return View(model);
        }

        _ctx.TasinmazKategoriCarpanlari.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Kategori çarpanı eklendi.";
        return RedirectToAction("Detay", "Tasinmaz", new { id = tasinmazId }, "tab-carpanlar");
    }

    [HttpGet("Carpanlar/Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.TasinmazCarpanPerm.Manage)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _ctx.TasinmazKategoriCarpanlari
            .Include(c => c.Tasinmaz)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return NotFound();

        await PopulateViewBagAsync();
        ViewBag.TasinmazId = entity.TasinmazId;
        ViewBag.TasinmazAd = entity.Tasinmaz.Ad;
        return View(entity);
    }

    [HttpPost("Carpanlar/Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.TasinmazCarpanPerm.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TasinmazKategoriCarpan model)
    {
        if (id != model.Id) return BadRequest();

        ModelState.Remove(nameof(model.Tasinmaz));
        ModelState.Remove(nameof(model.KiraciKategori));

        if (model.Carpan <= 0)
            ModelState.AddModelError(nameof(model.Carpan), "Çarpan 0'dan büyük olmalıdır.");

        if (ModelState.IsValid)
        {
            var mevcutVar = await _ctx.TasinmazKategoriCarpanlari
                .AnyAsync(c => c.TasinmazId == model.TasinmazId && c.KiraciKategoriId == model.KiraciKategoriId && c.Id != id);
            if (mevcutVar)
                ModelState.AddModelError(nameof(model.KiraciKategoriId), "Bu taşınmaz için seçili kategoride zaten bir çarpan tanımlı.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();
            var t = await _ctx.Tasinmazlar.FindAsync(model.TasinmazId);
            ViewBag.TasinmazId = model.TasinmazId;
            ViewBag.TasinmazAd = t?.Ad;
            return View(model);
        }

        _ctx.TasinmazKategoriCarpanlari.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Kategori çarpanı güncellendi.";
        return RedirectToAction("Detay", "Tasinmaz", new { id = model.TasinmazId }, "tab-carpanlar");
    }

    [HttpPost("Carpanlar/DurumDegistir/{id:int}")]
    [Authorize(Policy = PermissionCatalog.TasinmazCarpanPerm.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _ctx.TasinmazKategoriCarpanlari.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"Çarpan {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction("Detay", "Tasinmaz", new { id = entity.TasinmazId }, "tab-carpanlar");
    }
}
