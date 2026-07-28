using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/ReservationRateOverride")]
public class AdminReservationRateRuleController(
    IReservationService reservationService,
    IUnitService unitService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Index()
    {
        var liste = await reservationService.GetRateRulesAsync();
        return View(liste);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Create()
    {
        var vm = new ReservationRateOverrideViewModel
        {
            FreeDurationMinutes = 120,
            BillingPeriodMinutes = 60,
            KdvRate = 20,
            IsActive = true
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Create)]
    public async Task<IActionResult> Create(ReservationRateOverrideViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        try
        {
            await reservationService.SaveRateRuleAsync(new SaveReservationRateRuleInput(
                vm.Id,
                vm.UnitId,
                vm.FreeDurationMinutes,
                vm.BillingPeriodMinutes,
                vm.PeriodRate,
                vm.KdvRate,
                vm.Description,
                vm.IsActive
            ));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var kural = await reservationService.GetRateRuleByIdAsync(new GetRateRuleByIdInput(id));
        if (kural == null) return NotFound();

        var vm = new ReservationRateOverrideViewModel
        {
            Id = kural.Id,
            UnitId = kural.UnitId,
            FreeDurationMinutes = kural.FreeDurationMinutes,
            BillingPeriodMinutes = kural.BillingPeriodMinutes,
            PeriodRate = kural.PeriodRate,
            KdvRate = kural.KdvRate,
            IsActive = kural.IsActive,
            Description = kural.Description
        };
        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] ReservationRateOverrideViewModel vm)
    {
        vm.Id = id;

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        try
        {
            await reservationService.SaveRateRuleAsync(new SaveReservationRateRuleInput(
                vm.Id,
                vm.UnitId,
                vm.FreeDurationMinutes,
                vm.BillingPeriodMinutes,
                vm.PeriodRate,
                vm.KdvRate,
                vm.Description,
                vm.IsActive
            ));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ReservationRateRule.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await reservationService.ToggleRateRuleStatusAsync(new ToggleRateRuleStatusInput(id));
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(ReservationRateOverrideViewModel vm)
    {
        vm.ReservableUnits = await unitService.GetReservableUnitsAsync();
    }
}
