using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Document")]
public class DocumentController(
    IDocumentService documentService,
    IPermissionScopeCache permissionScopeCache,
    ICurrentUserContext currentUserContext) : Controller
{
    [HttpGet("Download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await documentService.DownloadAsync(
            new DownloadDocumentInput(id, await BuildAccessScopeAsync(canEdit: false)));

        return File(result.Content, result.Metadata.MimeType, result.Metadata.FileName);
    }

    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
                ?? "Belge bilgileri geçersiz.";
            throw new BusinessException(message);
        }

        using var memoryStream = new MemoryStream();
        await viewModel.File!.CopyToAsync(memoryStream);

        await documentService.UploadAsync(new UploadDocumentInput(
            viewModel.OwnerType,
            viewModel.OwnerId,
            viewModel.DocumentTypeId,
            viewModel.File.FileName,
            viewModel.File.ContentType,
            memoryStream.ToArray(),
            viewModel.Description,
            InvalidateOld: true,
            AccessScope: await BuildAccessScopeAsync(canEdit: true)));

        return RedirectToEntity(viewModel.OwnerType, viewModel.OwnerId);
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await documentService.DeleteAsync(
            new DeleteDocumentInput(id, await BuildAccessScopeAsync(canEdit: true)));

        return RedirectToEntity(result.OwnerType, result.OwnerId);
    }

    private async Task<DocumentAccessScopeInput> BuildAccessScopeAsync(bool canEdit)
    {
        var allowedOwnerTypes = new List<DocumentOwnerType>();

        if (canEdit
            ? User.HasPermission(PermissionCatalog.Tenant.Edit)
            : User.HasModuleAccess(PermissionCatalog.Tenant.Module))
            allowedOwnerTypes.Add(DocumentOwnerType.Tenant);

        if ((canEdit
                ? User.HasPermission(PermissionCatalog.Lease.Edit)
                : User.HasModuleAccess(PermissionCatalog.Lease.Module))
            || (!canEdit && User.HasModuleAccess(PermissionCatalog.TenantPortal.Lease.Module)))
            allowedOwnerTypes.Add(DocumentOwnerType.Lease);

        if ((canEdit
                ? User.HasPermission(PermissionCatalog.Payment.UploadReceipt)
                : User.HasModuleAccess(PermissionCatalog.Payment.Module))
            || (!canEdit && (User.HasModuleAccess(PermissionCatalog.TenantPortal.Payment.Module)
                || User.HasModuleAccess(PermissionCatalog.TenantPortal.Charge.Module))))
            allowedOwnerTypes.Add(DocumentOwnerType.Payment);

        IReadOnlyList<int>? propertyIds = null;
        IReadOnlyList<int>? unitIds = null;
        if (!currentUserContext.IsKiraciUser)
        {
            var scope = await permissionScopeCache.GetAsync(currentUserContext.UserId!);
            if (!scope.GlobalAccess)
            {
                propertyIds = scope.PropertyIds;
                unitIds = scope.UnitIds;
            }
        }

        return new DocumentAccessScopeInput(
            allowedOwnerTypes,
            propertyIds,
            unitIds,
            TenantId: currentUserContext.IsKiraciUser
                ? currentUserContext.TenantId
                : null);
    }

    private IActionResult RedirectToEntity(DocumentOwnerType ownerType, int ownerId) => ownerType switch
    {
        DocumentOwnerType.Tenant => RedirectToAction(nameof(TenantController.Details), "Tenant", new { id = ownerId }),
        DocumentOwnerType.Lease => RedirectToAction(nameof(LeaseController.Details), "Lease", new { id = ownerId }),
        DocumentOwnerType.Payment => RedirectToAction(nameof(PaymentController.Details), "Payment", new { id = ownerId }),
        _ => RedirectToAction(nameof(HomeController.Index), "Home")
    };
}
