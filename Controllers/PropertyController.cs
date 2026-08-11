using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class PropertyController(
    IPropertyService propertyService,
    IPropertyPricingService propertyPricingService,
    IRateHierarchyService rateHierarchyService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    private const int PropertyFormValueCountLimit = 10_000;

    [Authorize(Policy = PermissionCatalog.Property.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var properties = await propertyService.GetPagedAsync(
            new GetPropertiesPageInput(
                query,
                permissionScopeProvider.GlobalAccess
                    ? null
                    : permissionScopeProvider.AccessiblePropertyIds));

        ViewBag.Query = query;
        return View(properties);
    }

    [Authorize(Policy = PermissionCatalog.Property.Module)]
    public async Task<IActionResult> Details(int id)
    {
        if (!permissionScopeProvider.IsInScope(id)) return Forbid();

        var property = await propertyService.GetDetailsAsync(new GetPropertyDetailsInput(id));
        if (property == null) return NotFound();

        return View(new PropertyDetailsViewModel
        {
            Property = property,
            PricingMatrix = (await propertyPricingService.GetMatrixAsync(
                BuildPricingInput(id))).ToViewModel()
        });
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Property.Create)]
    public async Task<IActionResult> Create()
    {
        var viewModel = new CreatePropertyViewModel
        {
            PricingMatrix = (await propertyPricingService.GetMatrixAsync(
                BuildPricingInput(0))).ToViewModel()
        };
        await PrepareCommonFormAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(ValueCountLimit = PropertyFormValueCountLimit)]
    [Authorize(Policy = PermissionCatalog.Property.Create)]
    public async Task<IActionResult> Create(CreatePropertyViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PrepareCreateFormAsync(viewModel);
            return View(viewModel);
        }

        try
        {
            var createdProperty = await propertyService.CreateAsync(viewModel.ToInput());
            return RedirectToAction(nameof(Details), new { id = createdProperty.PropertyId });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PrepareCreateFormAsync(viewModel);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Property.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionScopeProvider.IsInScope(id)) return Forbid();

        var editData = await propertyService.GetForEditAsync(new GetPropertyForEditInput(id));
        if (editData == null) return NotFound();

        var viewModel = editData.ToViewModel();
        viewModel.PricingMatrix = (await propertyPricingService.GetMatrixAsync(
            BuildPricingInput(id))).ToViewModel();
        await PrepareCommonFormAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(ValueCountLimit = PropertyFormValueCountLimit)]
    [Authorize(Policy = PermissionCatalog.Property.Edit)]
    public async Task<IActionResult> Edit([FromForm] EditPropertyViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PrepareEditFormAsync(viewModel);
            return View(viewModel);
        }

        try
        {
            var accessiblePropertyIds = permissionScopeProvider.GlobalAccess
                ? null
                : permissionScopeProvider.AccessiblePropertyIds;
            await propertyService.UpdateWithChildrenAsync(
                viewModel.ToInput(accessiblePropertyIds));
            return RedirectToAction(nameof(Details), new { id = viewModel.Id });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PrepareEditFormAsync(viewModel);
            return View(viewModel);
        }
    }

    private async Task PrepareCreateFormAsync(CreatePropertyViewModel viewModel)
    {
        viewModel.PricingMatrix = await MergePricingMatrixAsync(viewModel.PricingMatrix, 0);
        await PrepareCommonFormAsync(viewModel);
    }

    private async Task PrepareEditFormAsync(EditPropertyViewModel viewModel)
    {
        viewModel.PricingMatrix = await MergePricingMatrixAsync(viewModel.PricingMatrix, viewModel.Id);

        var editData = await propertyService.GetForEditAsync(new GetPropertyForEditInput(viewModel.Id));
        if (editData != null)
        {
            viewModel.CanChangeUnitStructure = editData.CanChangeUnitStructure;
            foreach (var unit in viewModel.Units.Where(unit => unit.Id.HasValue))
                unit.HasActiveLease = editData.ActiveLeaseUnitIds.Contains(unit.Id!.Value);
            foreach (var area in viewModel.ReservationAreas.Where(area => area.Id.HasValue))
                area.HasActiveReservation = editData.ActiveReservationUnitIds.Contains(area.Id!.Value);
        }

        await PrepareCommonFormAsync(viewModel);
    }

    private async Task<PropertyPricingMatrixViewModel> MergePricingMatrixAsync(
        PropertyPricingMatrixViewModel? submittedMatrix,
        int propertyId)
    {
        var freshMatrix = (await propertyPricingService.GetMatrixAsync(
            BuildPricingInput(propertyId))).ToViewModel();
        if (submittedMatrix?.Rows == null) return freshMatrix;

        foreach (var freshRow in freshMatrix.Rows)
        {
            var submittedRow = submittedMatrix.Rows.FirstOrDefault(row =>
                row.TenantCategoryId == freshRow.TenantCategoryId);
            if (submittedRow == null) continue;

            foreach (var freshCell in freshRow.Cells)
            {
                var submittedCell = submittedRow.Cells.FirstOrDefault(cell =>
                    cell.ChargeTypeId == freshCell.ChargeTypeId);
                if (submittedCell == null) continue;

                freshCell.UnitValue = submittedCell.UnitValue;
                freshCell.CalculationMethod = submittedCell.CalculationMethod;
                freshCell.VatRate = submittedCell.VatRate;
            }
        }

        return freshMatrix;
    }

    private async Task PrepareCommonFormAsync(CreatePropertyViewModel viewModel)
    {
        await PopulateViewBagAsync();
        viewModel.ParentRate = await rateHierarchyService.GetParentForAsync(
            new GetParentRateInput(RateHierarchyLayer.Property, Year: DateTime.Now.Year));
        viewModel.ParentReservationRateOverride = await rateHierarchyService.GetReservationParentAsync(
            new GetParentReservationRateInput(DateTime.Now.Year));
    }

    private async Task PrepareCommonFormAsync(EditPropertyViewModel viewModel)
    {
        await PopulateViewBagAsync();
        viewModel.ParentRate = await rateHierarchyService.GetParentForAsync(
            new GetParentRateInput(RateHierarchyLayer.Property, Year: DateTime.Now.Year));
        viewModel.ParentReservationRateOverride = await rateHierarchyService.GetReservationParentAsync(
            new GetParentReservationRateInput(DateTime.Now.Year));
    }

    private async Task PopulateViewBagAsync()
    {
        var options = await propertyService.GetFormOptionsAsync();
        ViewBag.AllUnitTypes = options.UnitTypes;
        ViewBag.UnitTypes = options.UnitTypes
            .Where(unitType => unitType.Usage != UnitTypeUsage.Reservable)
            .ToList();
        ViewBag.ReservationUnitTypes = options.UnitTypes
            .Where(unitType => unitType.Usage == UnitTypeUsage.Reservable)
            .ToList();
        ViewBag.PropertyTypes = options.PropertyTypes;
        ViewBag.PropertyTypeUnitStructures = options.PropertyTypes.ToDictionary(
            propertyType => propertyType.Id,
            propertyType =>
            {
                var structures = new List<int>();
                if (propertyType.SupportsSingleUnit) structures.Add((int)UnitStructure.SingleUnit);
                if (propertyType.SupportsMultipleUnits) structures.Add((int)UnitStructure.MultipleUnits);
                return structures.ToArray();
            });
    }

    private GetPropertyPricingMatrixInput BuildPricingInput(int propertyId)
        => new(
            propertyId,
            PageSize: 100,
            AccessiblePropertyIds: propertyId == 0 || permissionScopeProvider.GlobalAccess
                ? null
                : permissionScopeProvider.AccessiblePropertyIds);
}
