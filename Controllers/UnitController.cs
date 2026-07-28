using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Unit")]
public class UnitController(
    IReservationService reservationService,
    IPermissionScopeProvider permissionScopeProvider,
    IUnitPricingService unitPricingService) : Controller
{
    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpGet("{id}/Rates")]
    public async Task<IActionResult> Rates([FromRoute(Name = "id")] int unitId)
        => View((await GetPricingDataAsync(unitId)).ToViewModel());

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id}/Rates")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rates(
        [FromRoute(Name = "id")] int unitId,
        UnitPricingFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(
                nameof(Rates),
                (await GetPricingDataAsync(unitId)).ToViewModel(viewModel.Rows));

        try
        {
            await unitPricingService.SavePricingMatrixAsync(
                viewModel.ToSaveInput(unitId, BuildPricingAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(
                nameof(Rates),
                (await GetPricingDataAsync(unitId)).ToViewModel(viewModel.Rows));
        }

        return RedirectToAction(nameof(Rates), new { id = unitId.ToHashId() });
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id}/SaveReservationRule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveReservationRule(
        [FromRoute(Name = "id")] int unitId,
        [FromForm(Name = "RuleId")] int ruleId,
        ReservationRateOverrideViewModel viewModel)
    {
        ModelState.Remove(nameof(viewModel.Id));
        ModelState.Remove(nameof(viewModel.UnitId));
        viewModel.UnitId = unitId;

        if (!ModelState.IsValid)
            return View(
                nameof(Rates),
                (await GetPricingDataAsync(unitId)).ToViewModel());

        try
        {
            await reservationService.SaveUnitReservationRateRuleAsync(
                new SaveUnitReservationRateRuleInput(
                    ruleId,
                    unitId,
                    viewModel.FreeDurationMinutes,
                    viewModel.BillingPeriodMinutes,
                    viewModel.PeriodRate,
                    viewModel.KdvRate,
                    viewModel.Description,
                    viewModel.IsActive,
                    BuildReservationAccessScope()));
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(
                nameof(Rates),
                (await GetPricingDataAsync(unitId)).ToViewModel());
        }

        return RedirectToAction(nameof(Rates), new { id = unitId.ToHashId() });
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id}/ClearReservationRule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearReservationRule(
        [FromRoute(Name = "id")] int unitId)
    {
        await reservationService.ClearUnitReservationRateRuleAsync(
            new ClearUnitReservationRateRuleInput(
                unitId,
                BuildReservationAccessScope()));

        return RedirectToAction(nameof(Rates), new { id = unitId.ToHashId() });
    }

    private Task<UnitPricingDataDto> GetPricingDataAsync(int unitId)
        => unitPricingService.GetPricingMatrixAsync(new GetUnitPricingInput(
            unitId,
            DateTime.Now.Year,
            BuildPricingAccessScope()));

    private UnitPricingAccessScopeInput BuildPricingAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new UnitPricingAccessScopeInput()
            : new UnitPricingAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);

    private ReservationAccessScopeInput BuildReservationAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new ReservationAccessScopeInput()
            : new ReservationAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}
