using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Reservation")]
public class ReservationController(
    IReservationService reservationService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var propertyIds = permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessiblePropertyIds;
        var unitIds = permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessibleUnitIds;

        var reservations = await reservationService.GetPageAsync(
            new GetReservationsPageInput(
                query,
                propertyIds,
                unitIds));
        var cancelledCount = await reservationService.GetCancelledCountAsync(
            new GetCancelledReservationCountInput(propertyIds, unitIds));

        ViewBag.Query = query;
        ViewBag.CancelledCount = cancelledCount;
        return View(reservations);
    }

    [HttpGet("Details/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var reservation = await reservationService.GetByIdAsync(
            new GetReservationByIdInput(id, GetAccessScope()));

        return View(reservation);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.Reservation.Create)]
    public async Task<IActionResult> Create(ReservationCreateQueryViewModel query)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var viewModel = new ReservationCreateViewModel
        {
            UnitId = query.UnitId,
            StartDate = DateTime.Today.AddHours(9),
            EndDate = DateTime.Today.AddHours(11)
        };
        await PopulateFormOptionsAsync(viewModel);

        return View(viewModel);
    }

    [HttpPost("Create")]
    [Authorize(Policy = PermissionCatalog.Reservation.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(viewModel);

            return View(viewModel);
        }

        try
        {
            await reservationService.CreateAsync(new CreateReservationInput(
                viewModel.UnitId!.Value,
                viewModel.TenantId!.Value,
                viewModel.StartDate,
                viewModel.EndDate,
                viewModel.Description,
                GetAccessScope()));

            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateFormOptionsAsync(viewModel);

            return View(viewModel);
        }
    }


    [HttpPost("Cancel/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Cancel)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancelReservationViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        await reservationService.CancelAsync(new CancelReservationInput(
            id,
            viewModel.Reason,
            GetAccessScope()));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("TransferToCharge/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.TransferToCharge)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferToCharge(int id)
    {
        await reservationService.TransferToChargeAsync(
            new TransferReservationToChargeInput(id, GetAccessScope()));

        return RedirectToAction(nameof(Index));
    }

    // AJAX: ücret önizleme
    [HttpGet("Calculate")]
    [Authorize(Policy = PermissionCatalog.Reservation.Create)]
    public async Task<IActionResult> Calculate(ReservationCalculationQueryViewModel query)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await reservationService.CalculateAsync(query.ToInput(GetAccessScope()));

        return Json(result);
    }

    private async Task PopulateFormOptionsAsync(ReservationCreateViewModel viewModel)
    {
        var options = await reservationService.GetFormOptionsAsync(
            new GetReservationFormOptionsInput(GetAccessScope()));

        viewModel.Units = options.Units;
        viewModel.Tenants = options.Tenants;
    }

    private ReservationAccessScopeInput GetAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new ReservationAccessScopeInput()
            : new ReservationAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}
