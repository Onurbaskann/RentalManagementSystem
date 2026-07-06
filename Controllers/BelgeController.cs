using KiraTakip.Authorization;
using KiraTakip.Extensions;
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
    public async Task<IActionResult> Yukle(BelgeOwnerTipi ownerType, int ownerId, int documentTypeId, IFormFile dosya, string? aciklama)
    {
        if (!User.HasPermission(GetRequiredPermission(ownerType)))
            return Forbid();

        if (dosya == null || dosya.Length == 0)
        {
            TempData["Error"] = "Dosya seçilmedi.";
            return RedirectToEntity(ownerType, ownerId);
        }

        var ext = Path.GetExtension(dosya.FileName).TrimStart('.').ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            TempData["Error"] = "Desteklenmeyen dosya türü.";
            return RedirectToEntity(ownerType, ownerId);
        }

        if (dosya.Length > 20 * 1024 * 1024)
        {
            TempData["Error"] = "Dosya boyutu 20 MB sınırını aşıyor.";
            return RedirectToEntity(ownerType, ownerId);
        }

        using var ms = new MemoryStream();
        await dosya.CopyToAsync(ms);

        await _belgeService.UploadAsync(
            ownerType, ownerId, documentTypeId,
            dosya.FileName, dosya.ContentType, ms.ToArray(), aciklama, invalidateOld: true);

        TempData["Success"] = "Belge yüklendi.";
        return RedirectToEntity(ownerType, ownerId);
    }

    [HttpPost("Sil/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, BelgeOwnerTipi ownerType, int ownerId)
    {
        if (!User.HasPermission(GetRequiredPermission(ownerType)))
            return Forbid();

        await _belgeService.DeleteAsync(id);
        TempData["Success"] = "Belge silindi.";
        return RedirectToEntity(ownerType, ownerId);
    }

    private static string GetRequiredPermission(BelgeOwnerTipi ownerType) => ownerType switch
    {
        BelgeOwnerTipi.Tenant   => PermissionCatalog.Tenant.Edit,
        BelgeOwnerTipi.Lease => PermissionCatalog.Lease.Edit,
        BelgeOwnerTipi.Odeme    => PermissionCatalog.Payment.UploadReceipt,
        _ => throw new ArgumentOutOfRangeException(nameof(ownerType))
    };

    private IActionResult RedirectToEntity(BelgeOwnerTipi ownerType, int ownerId) => ownerType switch
    {
        BelgeOwnerTipi.Tenant   => RedirectToAction("Detay", "Tenant",   new { id = ownerId }),
        BelgeOwnerTipi.Lease => RedirectToAction("Detay", "Lease", new { id = ownerId }),
        BelgeOwnerTipi.Odeme    => RedirectToAction("Detay", "Odeme",    new { id = ownerId }),
        _ => RedirectToAction("Index", "Home")
    };
}
