using HashidsNet;
using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.Feedback;
using KiraTakip.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Web.Controllers;

[Authorize]
[Route("UnitCapacity")]
public class UnitCapacityController(
    ICapacityPlanService capacityPlanService,
    IUnitService unitService,
    IPermissionScopeProvider permissionScopeProvider,
    ICurrentUserPermissionService permissionService,
    IHashids hashids) : Controller
{
    [Authorize(Policy = PermissionCatalog.Unit.Module)]
    [HttpGet("{id}")]
    public async Task<IActionResult> Index(
        [FromRoute(Name = "id")] int unitId,
        [FromQuery(Name = "k")] int? newCapacity)
    {
        var unit = await unitService.GetByIdAsync(unitId);
        if (unit?.Capacity is not > 0) return NotFound();

        var preview = await capacityPlanService.PreviewAsync(
            BuildInput(unitId, newCapacity is > 0 ? newCapacity.Value : unit.Capacity.Value, null));
        return View(await BuildPageAsync(unit, preview, []));
    }

    [Authorize(Policy = PermissionCatalog.Unit.Module)]
    [HttpPost("{id}/Preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(
        [FromRoute(Name = "id")] int unitId,
        [FromBody] UnitCapacityPreviewRequest request)
    {
        var preview = await capacityPlanService.PreviewAsync(
            BuildInput(unitId, request.NewCapacity, DecodeOverrides(request.Overrides), request.Optimize));
        return Json(preview.ToJson(hashids));
    }

    [Authorize(Policy = PermissionCatalog.Unit.UpdateCapacity)]
    [HttpPost("{id}/Apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        [FromRoute(Name = "id")] int unitId,
        UnitCapacityApplyViewModel viewModel)
    {
        var input = BuildInput(unitId, viewModel.NewCapacity, DecodeOverrides(viewModel.Overrides));

        try
        {
            await capacityPlanService.ApplyAsync(input, viewModel.ExpectedHash ?? string.Empty);
        }
        catch (BusinessException exception)
            when (exception is BusinessValidationException
                || exception.Code == CapacityPlanCodes.PreviewOutdated)
        {
            var field = exception is BusinessValidationException validation
                ? validation.Field
                : string.Empty;
            ModelState.AddModelError(field, exception.Message);

            var preview = await capacityPlanService.PreviewAsync(input);
            var failedUnit = await unitService.GetByIdAsync(unitId);
            return failedUnit == null
                ? NotFound()
                : View(nameof(Index), await BuildPageAsync(failedUnit, preview, viewModel.Overrides ?? []));
        }

        TempData[FeedbackTempDataKeys.SuccessMessage] = "Kontenjan güncellendi.";
        var unit = await unitService.GetByIdAsync(unitId);
        return unit == null
            ? RedirectToAction(nameof(Index), new { id = unitId.ToHashId() })
            : RedirectToAction("Details", "Property", new { id = unit.PropertyId.ToHashId() });
    }

    private async Task<UnitCapacityPageViewModel> BuildPageAsync(
        UnitDetailDto unit,
        CapacityPlanPreviewDto preview,
        Dictionary<string, decimal> overrides)
    {
        return new UnitCapacityPageViewModel
        {
            UnitId = unit.Id,
            UnitName = string.IsNullOrWhiteSpace(unit.UnitNo) ? unit.Name : $"{unit.UnitNo} - {unit.Name}",
            PropertyName = unit.PropertyName,
            PropertyId = unit.PropertyId,
            CanApply = await permissionService.HasPermissionAsync(PermissionCatalog.Unit.UpdateCapacity),
            Preview = preview.ToJson(hashids),
            Overrides = overrides
        };
    }

    private Dictionary<int, decimal>? DecodeOverrides(Dictionary<string, decimal>? raw)
    {
        if (raw == null || raw.Count == 0) return null;

        var result = new Dictionary<int, decimal>();
        foreach (var (key, value) in raw)
        {
            var decoded = hashids.Decode(key);
            if (decoded.Length == 0)
                throw new BusinessException("Geçersiz sözleşme kimliği.", ErrorType.Failure, "Capacity.InvalidLeaseId");
            result[decoded[0]] = value;
        }

        return result;
    }

    private CapacityPlanInput BuildInput(
        int unitId,
        int newCapacity,
        IReadOnlyDictionary<int, decimal>? overrides,
        bool optimize = false)
        => new(unitId, newCapacity, BuildAccessScope(), overrides, optimize);

    private LeaseAccessScopeInput BuildAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new LeaseAccessScopeInput()
            : new LeaseAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}
