using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Infrastructure;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Tenant")]
public class TenantController(
    ITenantService tenantService,
    ILeaseService leaseService,
    IPermissionScopeProvider permissionScopeProvider,
    IDocumentService documentService,
    ITenantUserService tenantUserService,
    ITenantCategoryService tenantCategoryService,
    ISectorService sectorService,
    ICurrentUserContext currentUserContext) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Tenant.Module)]
    public async Task<IActionResult> Index()
    {
        var accessScope = BuildAccessScope();
        var tenants = await tenantService.GetAllAsync(
            new GetTenantsInput(accessScope.PropertyIds, accessScope.UnitIds));
        var leases = await leaseService.GetAllAsync(new GetLeasesInput(
            PropertyIds: accessScope.PropertyIds,
            UnitIds: accessScope.UnitIds));
        var now = DateTime.Now;

        return View(new TenantIndexViewModel
        {
            Tenants = tenants,
            ActiveLeaseCounts = leases
                .Where(lease => lease.Status == LeaseStatus.Active
                    && lease.StartDate <= now
                    && lease.EndDate >= now)
                .GroupBy(lease => lease.TenantId)
                .ToDictionary(group => group.Key, group => group.Count()),
            CanCreate = permissionScopeProvider.GlobalAccess
                && User.HasPermission(PermissionCatalog.Tenant.Create),
            CanEdit = User.HasPermission(PermissionCatalog.Tenant.Edit)
        });
    }

    [HttpGet("Details/{id}")]
    [Authorize(Policy = PermissionCatalog.Tenant.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var accessScope = BuildAccessScope();
        var tenant = await tenantService.GetDetailsAsync(
            new GetTenantDetailsInput(id, accessScope));
        if (tenant == null)
            return permissionScopeProvider.GlobalAccess ? NotFound() : Forbid();

        var leases = await leaseService.GetByTenantAsync(new GetLeasesByTenantInput(
            id,
            new LeaseAccessScopeInput(accessScope.PropertyIds, accessScope.UnitIds)));

        var depositAmounts = await leaseService.GetDepositsAsync(
            new GetLeaseDepositsInput(leases.Select(lease => lease.Id).ToList()));
        var documents = await documentService.GetListAsync(
            new GetDocumentsInput(
                DocumentOwnerType.Tenant,
                id,
                new DocumentAccessScopeInput(
                    [DocumentOwnerType.Tenant],
                    accessScope.PropertyIds,
                    accessScope.UnitIds)));
        var documentTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Tenant));

        return View(new TenantDetailsViewModel
        {
            Tenant = tenant,
            Leases = leases,
            DepositAmounts = depositAmounts,
            Documents = documents,
            DocumentTypes = documentTypes
        });
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.Tenant.Create)]
    public async Task<IActionResult> Create()
    {
        if (!permissionScopeProvider.GlobalAccess) return Forbid();

        var viewModel = new TenantFormViewModel
        {
            TenantNo = await tenantService.GenerateTenantNoAsync()
        };
        await PopulateTenantFormOptionsAsync(viewModel);

        return View(viewModel);
    }

    [HttpPost("Create")]
    [Authorize(Policy = PermissionCatalog.Tenant.Create)]
    public async Task<IActionResult> Create(TenantFormViewModel viewModel)
    {
        if (!permissionScopeProvider.GlobalAccess) return Forbid();

        await PopulateDocumentTypesAsync(viewModel);

        if (!ModelState.IsValid)
        {
            await PopulateCategoryAndSectorOptionsAsync(viewModel);
            return View(viewModel);
        }

        var documents = new List<TenantDocumentUploadInput>();
        foreach (var documentType in viewModel.DocumentTypes)
        {
            var file = Request.Form.Files.GetFile($"dosya_{documentType.Id}");
            if (file == null || file.Length == 0) continue;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            documents.Add(new TenantDocumentUploadInput(documentType.Id, file.FileName, file.ContentType, stream.ToArray()));
        }

        CreatedTenantDto result;
        try
        {
            result = await tenantService.CreateAsync(
                ToCreateInput(viewModel, documents, BuildAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateTenantFormOptionsAsync(viewModel);
            return View(viewModel);
        }
        catch (BusinessException exception) when (
            exception.ErrorType is ErrorType.Failure or ErrorType.Conflict)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PopulateTenantFormOptionsAsync(viewModel);
            return View(viewModel);
        }

        if (!string.IsNullOrWhiteSpace(viewModel.InitialRepresentativeEmail))
        {
            var invitationResult = await tenantUserService.TrySendInitialRepresentativeInvitationAsync(
                new SendInitialTenantRepresentativeInput(
                    result.TenantId,
                    viewModel.InitialRepresentativeEmail,
                    viewModel.InitialRepresentativeFullName,
                    currentUserContext.UserId!));

            if (!invitationResult.Sent)
            {
                TempData[FeedbackTempDataKeys.SuccessMessage] =
                    $"'{result.DisplayName}' kiracı kaydı başarıyla oluşturuldu.";
                TempData["Error"] = invitationResult.Error;
            }
        }

        return RedirectToAction(nameof(Details), new { id = result.TenantId });
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Tenant.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var tenant = await tenantService.GetDetailsAsync(
            new GetTenantDetailsInput(id, BuildAccessScope()));
        if (tenant == null)
            return permissionScopeProvider.GlobalAccess ? NotFound() : Forbid();

        var viewModel = new TenantFormViewModel
        {
            Id = tenant.Id,
            TenantNo = tenant.TenantNo,
            Name = tenant.Name,
            TradeRegistryNo = tenant.TradeRegistryNo,
            TaxNo = tenant.TaxNo,
            TaxOffice = tenant.TaxOffice,
            MersisNo = tenant.MersisNo,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Address = tenant.Address,
            TenantCategoryId = tenant.TenantCategoryId,
            SectorId = tenant.SectorId
        };
        await PopulateTenantFormOptionsAsync(viewModel);

        return View(viewModel);
    }

    [HttpPost("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Tenant.Edit)]
    public async Task<IActionResult> Edit(int id, TenantFormViewModel viewModel)
    {
        if (id != viewModel.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateTenantFormOptionsAsync(viewModel);
            return View(viewModel);
        }

        try
        {
            await tenantService.UpdateAsync(
                ToUpdateInput(id, viewModel, BuildAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateTenantFormOptionsAsync(viewModel);
            return View(viewModel);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateTenantFormOptionsAsync(TenantFormViewModel viewModel)
    {
        await PopulateCategoryAndSectorOptionsAsync(viewModel);
        await PopulateDocumentTypesAsync(viewModel);
    }

    private async Task PopulateCategoryAndSectorOptionsAsync(TenantFormViewModel viewModel)
    {
        viewModel.TenantCategories = (await tenantCategoryService.GetTenantCategoriesAsync())
            .Where(category => category.IsActive)
            .ToList();
        viewModel.Sectors = (await sectorService.GetSectorsAsync())
            .Where(sector => sector.IsActive)
            .ToList();
    }

    private async Task PopulateDocumentTypesAsync(TenantFormViewModel viewModel)
    {
        viewModel.DocumentTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Tenant));
    }

    private static CreateTenantInput ToCreateInput(
        TenantFormViewModel viewModel,
        IReadOnlyList<TenantDocumentUploadInput> documents,
        TenantAccessScopeInput accessScope)
        => new(
            viewModel.TenantNo,
            viewModel.Name!,
            viewModel.TradeRegistryNo,
            viewModel.TaxNo,
            viewModel.TaxOffice,
            viewModel.MersisNo,
            viewModel.Phone,
            viewModel.Email,
            viewModel.Address,
            viewModel.TenantCategoryId,
            viewModel.SectorId,
            documents,
            accessScope);

    private static UpdateTenantInput ToUpdateInput(
        int id,
        TenantFormViewModel viewModel,
        TenantAccessScopeInput accessScope)
        => new(
            id,
            viewModel.TenantNo,
            viewModel.Name!,
            viewModel.TradeRegistryNo,
            viewModel.TaxNo,
            viewModel.TaxOffice,
            viewModel.MersisNo,
            viewModel.Phone,
            viewModel.Email,
            viewModel.Address,
            viewModel.TenantCategoryId,
            viewModel.SectorId,
            accessScope);

    private TenantAccessScopeInput BuildAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new TenantAccessScopeInput()
            : new TenantAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}
