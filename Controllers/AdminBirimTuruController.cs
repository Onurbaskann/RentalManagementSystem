using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Extensions;

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
    public async Task<IActionResult> Create()
    {
        var nextSira = (_ctx.BirimTurleri.Max(b => (int?)b.Sira) ?? 0) + 1;
        await PopulateBorcTipleriAsync();
        return View(new BirimTuru { Sira = nextSira, OlusturmaTarihi = DateTime.UtcNow });
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BirimTuru model)
    {
        if (model.RezervasyonYapilabilirMi && (!model.BorcTipiId.HasValue || model.BorcTipiId <= 0))
            ModelState.AddModelError(nameof(model.BorcTipiId), "Rezervasyon birim türü için borç tipi seçilmelidir.");

        if (!ModelState.IsValid)
        {
            await PopulateBorcTipleriAsync();
            return View(model);
        }

        if (model.KiralanabilirMi == model.RezervasyonYapilabilirMi)
        {
            ModelState.AddModelError(string.Empty,
                "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Rezervasyon yapılabilir.");
            await PopulateBorcTipleriAsync();
            return View(model);
        }

        if (model.KiralanabilirMi) model.BorcTipiId = null;

        model.Kod = model.Kod.ToSafeCode();
        if (await _ctx.BirimTurleri.AnyAsync(b => b.Kod == model.Kod))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            await PopulateBorcTipleriAsync();
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
        await PopulateBorcTipleriAsync();
        return View(entity);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BirimTuru model)
    {
        if (id != model.Id) return BadRequest();
        
        if (model.RezervasyonYapilabilirMi && (!model.BorcTipiId.HasValue || model.BorcTipiId <= 0))
            ModelState.AddModelError(nameof(model.BorcTipiId), "Rezervasyon birim türü için borç tipi seçilmelidir.");

        if (!ModelState.IsValid)
        {
            await PopulateBorcTipleriAsync();
            return View(model);
        }

        if (model.KiralanabilirMi == model.RezervasyonYapilabilirMi)
        {
            ModelState.AddModelError(string.Empty,
                "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Rezervasyon yapılabilir.");
            await PopulateBorcTipleriAsync();
            return View(model);
        }

        if (model.KiralanabilirMi) model.BorcTipiId = null;

        model.Kod = model.Kod.ToSafeCode();
        if (await _ctx.BirimTurleri.AnyAsync(b => b.Kod == model.Kod && b.Id != id))
        {
            ModelState.AddModelError(nameof(model.Kod), "Bu kod zaten kullanılıyor.");
            await PopulateBorcTipleriAsync();
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

        if (entity.Aktif) // Pasife çekme
        {
            // 1. Bağlı Birim'ler aracılığıyla aktif tahakkuk kontrolü
            var aktifTahakkukVar = await _ctx.KiraTahakkuklar
                .AnyAsync(t => t.Durum != TahakkukDurumu.TamOdendi && 
                               t.Durum != TahakkukDurumu.IptalEdildi &&
                               _ctx.Birimler.Any(b => b.BirimTuruId == id && 
                                                      (_ctx.Sozlesmeler.Any(s => s.BirimId == b.Id && s.Id == t.KiraSozlesmesiId) ||
                                                       _ctx.Rezervasyonlari.Any(r => r.BirimId == b.Id && r.KiraTahakkukId == t.Id))));
            
            if (aktifTahakkukVar)
            {
                TempData["Error"] = "Bu birim türüne bağlı birimlerde aktif tahakkuk bulunduğu için pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Aktif rezervasyon kontrolü
            var aktifRezervasyonVar = await _ctx.Rezervasyonlari
                .AnyAsync(r => r.Durum == RezervasyonDurumu.Planlandi && 
                               _ctx.Birimler.Any(b => b.BirimTuruId == id && b.Id == r.BirimId));

            if (aktifRezervasyonVar)
            {
                TempData["Error"] = "Bu birim türüne bağlı birimlerde planlanmış rezervasyon bulunduğu için pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Cascade BorcTipi pasif (başka aktif BirimTuru kullanmıyorsa)
            if (entity.BorcTipiId.HasValue)
            {
                var baskaKullananVar = await _ctx.BirimTurleri
                    .AnyAsync(b => b.BorcTipiId == entity.BorcTipiId && b.Id != id && b.Aktif);

                if (!baskaKullananVar)
                {
                    var borcTipi = await _ctx.BorcTipleri.FindAsync(entity.BorcTipiId.Value);
                    if (borcTipi != null) borcTipi.Aktif = false;
                }
            }
        }

        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Ad}' {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateBorcTipleriAsync()
    {
        ViewBag.BorcTipiAdaylari = await _ctx.BorcTipleri
            .Where(b => b.Davranis == BorcTipiDavranisi.RezervasyonOzel && b.Aktif)
            .OrderBy(b => b.Sira).ThenBy(b => b.Ad)
            .ToListAsync();
    }
}
