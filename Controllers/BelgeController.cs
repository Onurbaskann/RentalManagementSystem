using KiraTakip.Authorization;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Belge")]
public class BelgeController : Controller
{
    private readonly IBelgeService _belgeService;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { "pdf", "jpg", "jpeg", "png", "doc", "docx", "xls", "xlsx" };

    public BelgeController(IBelgeService belgeService)
    {
        _belgeService = belgeService;
    }

    [HttpGet("Indir/{id:int}")]
    public async Task<IActionResult> Indir(int id)
    {
        try
        {
            var (meta, icerik) = await _belgeService.DownloadAsync(id);
            return File(icerik, meta.MimeType, meta.DosyaAdi);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("Yukle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Kiraci.Edit)]
    public async Task<IActionResult> Yukle(int kiraciId, int belgeTuruId, IFormFile dosya, string? aciklama)
    {
        if (dosya == null || dosya.Length == 0)
            return BadRequest("Dosya seçilmedi.");

        var ext = Path.GetExtension(dosya.FileName).TrimStart('.').ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest("Desteklenmeyen dosya türü.");

        if (dosya.Length > 20 * 1024 * 1024)
            return BadRequest("Dosya boyutu 20 MB sınırını aşıyor.");

        using var ms = new MemoryStream();
        await dosya.CopyToAsync(ms);

        await _belgeService.UploadAsync(
            BelgeOwnerTipi.Kiraci, kiraciId, belgeTuruId,
            dosya.FileName, dosya.ContentType, ms.ToArray(), aciklama);

        TempData["Success"] = "Belge yüklendi.";
        return RedirectToAction("Detay", "Kiraci", new { id = kiraciId });
    }

    [HttpPost("Sil/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Kiraci.Edit)]
    public async Task<IActionResult> Sil(int id, int kiraciId)
    {
        await _belgeService.DeleteAsync(id);
        TempData["Success"] = "Belge silindi.";
        return RedirectToAction("Detay", "Kiraci", new { id = kiraciId });
    }
}
