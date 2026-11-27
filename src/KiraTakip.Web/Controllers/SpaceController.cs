using KiraTakip.Authorization;
using KiraTakip.Domain.Spaces;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Tenants;
using KiraTakip.Web.Feedback;
using KiraTakip.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Web.Controllers;

[Authorize]
[Route("Space")]
public class SpaceController(
    ISpaceService spaceService,
    ISpaceTypeService spaceTypeService,
    ISpaceAttributeService spaceAttributeService,
    ISpaceRateService spaceRateService,
    ITenantCategoryService tenantCategoryService,
    IChargeTypeService chargeTypeService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    private SpaceAccessScope Scope => permissionScopeProvider.SpaceScope;

    private const string NodeQuery = "node";

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Space.Module)]
    public async Task<IActionResult> Index()
        => View(await BuildIndexAsync(new SpaceFormViewModel()));

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionCatalog.Space.Module)]
    public async Task<IActionResult> Tree(int id, [FromQuery(Name = NodeQuery)] int? nodeId)
    {
        var tree = await spaceService.GetTreeAsync(id, Scope);
        var selectedId = nodeId ?? id;
        if (!Contains(tree, selectedId)) return NotFound();

        var selected = await spaceService.GetDetailAsync(selectedId, Scope);
        var attributes = await spaceAttributeService.GetListAsync();
        var values = await spaceAttributeService.GetValuesAsync(selectedId);
        var categories = await tenantCategoryService.GetTenantCategoriesAsync();
        var chargeTypes = await chargeTypeService.GetListAsync();

        var model = new SpaceTreeViewModel
        {
            Tree = tree,
            Selected = selected,
            Types = (await spaceTypeService.GetListAsync()).Where(t => t.IsActive || t.Id == selected.SpaceTypeId).ToList(),
            History = await spaceService.GetHistoryAsync(selectedId, Scope),
            Address = selected.ParentId == null ? await spaceService.GetAddressAsync(selectedId, Scope) : null,
            Attributes = attributes.Where(a => a.IsActive || values.Any(v => v.AttributeDefinitionId == a.Id)).ToList(),
            AttributeValues = values.ToDictionary(v => v.AttributeDefinitionId, v => v.Value),
            Rates = await spaceRateService.GetListAsync(selectedId),
            TenantCategoryNames = categories.ToDictionary(c => c.Id, c => c.Name),
            ChargeTypeNames = chargeTypes.ToDictionary(c => c.Id, c => c.Name),
            ChildForm = new SpaceFormViewModel { RootId = id, ParentId = selectedId }
        };
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.Create)]
    public async Task<IActionResult> Create(SpaceFormViewModel model)
    {
        try
        {
            var address = model.ParentId == null
                ? new SpaceAddressInput(model.City, model.District, model.Neighborhood, model.Address)
                : null;
            var result = await spaceService.CreateAsync(new CreateSpaceInput(
                model.ParentId, model.SpaceTypeId, model.Code, model.Name, model.SortOrder, model.Description,
                model.Area, model.Usage, model.IsPreAllocatable, model.IsShared, address), Scope);
            SetSuccess("Alan eklendi.", result.Warnings);
            var rootId = model.ParentId == null ? result.Id : model.RootId;
            return RedirectToTree(rootId, result.Id);
        }
        catch (BusinessValidationException exception) when (model.ParentId == null)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(nameof(Index), await BuildIndexAsync(model));
        }
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.Edit)]
    public async Task<IActionResult> Edit(int id, SpaceFormViewModel model)
    {
        var result = await spaceService.UpdateAsync(new UpdateSpaceInput(
            id, model.ParentId, model.SpaceTypeId, model.Name, model.SortOrder, model.Description,
            model.Area, model.Usage, model.IsPreAllocatable, model.IsShared), Scope);
        SetSuccess("Alan güncellendi.", result.Warnings);
        return RedirectToTree(model.RootId, id);
    }

    [HttpPost("Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.Deactivate)]
    public async Task<IActionResult> Deactivate(int id, int rootId)
    {
        await spaceService.DeactivateAsync(id, Scope);
        SetSuccess("Alan ve alt alanları pasifleştirildi.");
        return RedirectToTree(rootId, id);
    }

    [HttpPost("Activate/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.Activate)]
    public async Task<IActionResult> Activate(int id, int rootId)
    {
        await spaceService.ActivateAsync(id, Scope);
        SetSuccess("Alan etkinleştirildi. Alt alanlar tek tek etkinleştirilmelidir.");
        return RedirectToTree(rootId, id);
    }

    [HttpGet("Delete/{id}")]
    [Authorize(Policy = PermissionCatalog.Space.Module)]
    public async Task<IActionResult> DeletePreview(int id, int rootId)
        => View("Delete", new SpaceDeleteViewModel
        {
            RootId = rootId,
            Detail = await spaceService.GetDetailAsync(id, Scope),
            Preview = await spaceService.GetDeletePreviewAsync(id, Scope)
        });

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.Delete)]
    public async Task<IActionResult> Delete(int id, int rootId)
    {
        await spaceService.DeleteAsync(id, Scope);
        SetSuccess("Alan silindi.");
        return id == rootId
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Tree), new { id = rootId.ToHashId() });
    }

    [HttpPost("EditRate/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.EditRate)]
    public async Task<IActionResult> EditRate(int id, SpaceRateFormViewModel model)
    {
        await spaceService.EnsureInScopeAsync(id, Scope);
        if (model.Operation == SpaceRateOperations.Delete)
        {
            var rates = await spaceRateService.GetListAsync(id);
            if (rates.All(r => r.Id != model.RateId)) return NotFound();
            await spaceRateService.DeleteAsync(model.RateId);
            SetSuccess("Tarife silindi.");
        }
        else
        {
            await spaceRateService.SaveAsync(new SpaceRateInput(
                id, model.TenantCategoryId, model.ChargeTypeId, model.CalculationMethod, model.UnitValue, model.KdvRate));
            SetSuccess("Tarife kaydedildi.");
        }
        return RedirectToTree(model.RootId, id);
    }

    [HttpPost("EditAddress/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.EditAddress)]
    public async Task<IActionResult> EditAddress(int id, SpaceAddressFormViewModel model)
    {
        await spaceService.UpdateAddressAsync(id, new SpaceAddressInput(model.City, model.District, model.Neighborhood, model.Address), Scope);
        SetSuccess("Adres güncellendi.");
        return RedirectToTree(model.RootId, id);
    }

    [HttpPost("EditAttributes/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Space.EditAttributes)]
    public async Task<IActionResult> EditAttributes(int id, SpaceAttributesFormViewModel model)
    {
        await spaceService.EnsureInScopeAsync(id, Scope);
        await spaceAttributeService.SaveValuesAsync(
            id, model.Values.Select(v => new SpaceAttributeValueInput(v.AttributeDefinitionId, v.Value)).ToList());
        SetSuccess("Özellikler kaydedildi.");
        return RedirectToTree(model.RootId, id);
    }

    private async Task<SpaceIndexViewModel> BuildIndexAsync(SpaceFormViewModel form)
        => new()
        {
            Roots = await spaceService.GetRootsAsync(Scope),
            Types = (await spaceTypeService.GetListAsync()).Where(t => t.IsActive).ToList(),
            Form = form
        };

    private RedirectToActionResult RedirectToTree(int rootId, int nodeId)
        => RedirectToAction(nameof(Tree), new { id = rootId.ToHashId(), node = nodeId.ToHashId() });

    private void SetSuccess(string message, IReadOnlyList<SpaceChangeWarning>? warnings = null)
    {
        TempData[FeedbackTempDataKeys.SuccessMessage] = message;
        if (warnings is { Count: > 0 })
            TempData[FeedbackTempDataKeys.WarningMessage] = string.Join(" ", warnings.Select(SpaceDisplay.Warning));
    }

    private static bool Contains(SpaceTreeNodeDto node, int id)
        => node.Id == id || node.Children.Any(c => Contains(c, id));
}
