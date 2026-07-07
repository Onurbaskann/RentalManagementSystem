using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Belge")]
public class DocumentController : Controller
{
    private readonly IDocumentService _documentService;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { "pdf", "jpg", "jpeg", "png", "doc", "docx", "xls", "xlsx" };

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("Indir/{id:int}")]
    public async Task<IActionResult> Indir(int id)
    {
        try
        {
            var (meta, content) = await _documentService.DownloadAsync(id);
            return File(content, meta.MimeType, meta.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("Yukle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yukle(DocumentOwnerType ownerType, int ownerId, int documentTypeId, IFormFile dosya, string? aciklama)
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

        await _documentService.UploadAsync(
            ownerType, ownerId, documentTypeId,
            dosya.FileName, dosya.ContentType, ms.ToArray(), aciklama, invalidateOld: true);

        TempData["Success"] = "Belge yüklendi.";
        return RedirectToEntity(ownerType, ownerId);
    }

    [HttpPost("Sil/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, DocumentOwnerType ownerType, int ownerId)
    {
        if (!User.HasPermission(GetRequiredPermission(ownerType)))
            return Forbid();

        await _documentService.DeleteAsync(id);
        TempData["Success"] = "Belge silindi.";
        return RedirectToEntity(ownerType, ownerId);
    }

    private static string GetRequiredPermission(DocumentOwnerType ownerType) => ownerType switch
    {
        DocumentOwnerType.Tenant   => PermissionCatalog.Tenant.Edit,
        DocumentOwnerType.Lease => PermissionCatalog.Lease.Edit,
        DocumentOwnerType.Payment    => PermissionCatalog.Payment.UploadReceipt,
        _ => throw new ArgumentOutOfRangeException(nameof(ownerType))
    };

    private IActionResult RedirectToEntity(DocumentOwnerType ownerType, int ownerId) => ownerType switch
    {
        DocumentOwnerType.Tenant   => RedirectToAction("Detay", "Tenant",   new { id = ownerId }),
        DocumentOwnerType.Lease => RedirectToAction("Detay", "Lease", new { id = ownerId }),
        DocumentOwnerType.Payment    => RedirectToAction("Detay", "Payment",    new { id = ownerId }),
        _ => RedirectToAction("Index", "Home")
    };
}
