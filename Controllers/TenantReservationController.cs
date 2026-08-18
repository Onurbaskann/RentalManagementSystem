using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Extensions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[Authorize(Policy = PermissionCatalog.TenantPortal.Reservation.Module)]
[RequireKiraciId]
[Route("Tenant/MyReservations")]
public class TenantReservationController(
    IReservationService reservationService,
    ICurrentUserContext currentUserContext,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var list = await reservationService.GetTenantReservationsPageAsync(
            new GetTenantReservationsPageInput(
                tenantId,
                query,
                permissionScopeProvider.GlobalAccess
                    ? new ReservationAccessScopeInput()
                    : new ReservationAccessScopeInput(
                        permissionScopeProvider.AccessiblePropertyIds,
                        permissionScopeProvider.AccessibleUnitIds)));
        ViewBag.Query = query;
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.Reservation.Create)]
    public async Task<IActionResult> Create()
    {
        var viewModel = new TenantReservationCreateViewModel
        {
            StartDate = DateTime.Today.AddDays(1).AddHours(9),
            EndDate = DateTime.Today.AddDays(1).AddHours(10)
        };
        await PopulateCreateOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost("Create")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.Reservation.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantReservationCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateOptionsAsync(viewModel);
            return View(viewModel);
        }

        try
        {
            await reservationService.CreateRequestAsync(
                viewModel.ToInput(currentUserContext, GetAccessScope()));
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateCreateOptionsAsync(viewModel);
            return View(viewModel);
        }
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
        => View(await reservationService.GetTenantByIdAsync(
            new GetTenantReservationByIdInput(
                id,
                currentUserContext.TenantId!.Value,
                GetAccessScope())));

    [HttpPost("Cancel/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.Reservation.Cancel)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancelReservationViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(nameof(Details), await reservationService.GetTenantByIdAsync(
                new GetTenantReservationByIdInput(
                    id,
                    currentUserContext.TenantId!.Value,
                    GetAccessScope())));

        await reservationService.CancelTenantAsync(new CancelTenantReservationInput(
            id,
            currentUserContext.TenantId!.Value,
            viewModel.Reason,
            GetAccessScope(),
            currentUserContext.UserId!));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Calendar")]
    public async Task<IActionResult> Calendar([FromQuery] ReservationCalendarQueryViewModel query)
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var scope = permissionScopeProvider.GlobalAccess
            ? new ReservationAccessScopeInput()
            : new ReservationAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);

        return View(await reservationService.GetTenantCalendarAsync(
            query.ToTenantInput(tenantId, scope)));
    }

    [HttpGet("Availability")]
    public async Task<IActionResult> Availability([FromQuery] ReservationAvailabilityQueryViewModel query)
    {
        var scope = permissionScopeProvider.GlobalAccess
            ? new ReservationAccessScopeInput()
            : new ReservationAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
        if (!query.TryMap(scope, out var input))
            return BadRequest(new { code = "RESERVATION_AVAILABILITY_INPUT_REQUIRED", message = "Birim, başlangıç ve bitiş zamanı zorunludur." });

        return Json(await reservationService.CheckAvailabilityAsync(input!));
    }

    private async Task PopulateCreateOptionsAsync(TenantReservationCreateViewModel viewModel)
    {
        var options = await reservationService.GetFormOptionsAsync(
            new GetReservationFormOptionsInput(GetAccessScope()));
        viewModel.Units = options.Units;
    }

    private ReservationAccessScopeInput GetAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new ReservationAccessScopeInput()
            : new ReservationAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}
