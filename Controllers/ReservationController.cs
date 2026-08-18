using KiraTakip.Authorization;
using KiraTakip.Extensions;
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
    IPermissionScopeProvider permissionScopeProvider,
    ICurrentUserContext currentUserContext) : Controller
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

    [HttpGet("Calendar")]
    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Calendar([FromQuery] ReservationCalendarQueryViewModel query)
        => View(await reservationService.GetCalendarAsync(query.ToInternalInput(GetAccessScope())));

    [HttpGet("Availability")]
    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Availability([FromQuery] ReservationAvailabilityQueryViewModel query)
    {
        if (!query.TryMap(GetAccessScope(), out var input))
            return BadRequest(new { code = "RESERVATION_AVAILABILITY_INPUT_REQUIRED", message = "Birim, başlangıç ve bitiş zamanı zorunludur." });

        return Json(await reservationService.CheckAvailabilityAsync(input!));
    }

    [HttpGet("Details/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var reservation = await reservationService.GetByIdAsync(
            new GetReservationByIdInput(id, GetAccessScope()));

        return View(reservation);
    }

    [HttpPost("Approve/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Approve)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, ApproveReservationViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(nameof(Details), await reservationService.GetByIdAsync(
                new GetReservationByIdInput(id, GetAccessScope())));

        await reservationService.ApproveAsync(new ApproveReservationInput(
            id,
            viewModel.RowVersion,
            currentUserContext.UserId!,
            GetAccessScope()));
        return RedirectToAction(nameof(Details), new { id = id.ToHashId() });
    }

    [HttpPost("Reject/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Reject)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, RejectReservationViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(nameof(Details), await reservationService.GetByIdAsync(
                new GetReservationByIdInput(id, GetAccessScope())));

        await reservationService.RejectAsync(new RejectReservationInput(
            id,
            viewModel.Reason!,
            viewModel.RowVersion,
            currentUserContext.UserId!,
            GetAccessScope()));
        return RedirectToAction(nameof(Details), new { id = id.ToHashId() });
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
            StartDate = DateTime.Today.AddDays(1).AddHours(9),
            EndDate = DateTime.Today.AddDays(1).AddHours(11)
        };
        await PopulateFormOptionsAsync(viewModel);

        return View(viewModel);
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var reservation = await reservationService.GetByIdAsync(
            new GetReservationByIdInput(id, GetAccessScope()));
        var viewModel = new ReservationEditViewModel
        {
            Id = reservation.Id,
            UnitId = reservation.UnitId,
            TenantId = reservation.TenantId,
            StartDate = reservation.StartDate,
            EndDate = reservation.EndDate,
            Title = reservation.Title,
            Description = reservation.Description,
            Notes = reservation.Notes,
            InternalNotes = reservation.InternalNotes,
            RowVersion = reservation.RowVersion,
            Attendees = reservation.Attendees
                .Where(attendee => !attendee.IsReservationOwner)
                .Select(attendee => new ReservationAttendeeInputViewModel
                {
                    DisplayName = attendee.DisplayName,
                    EmailAddress = attendee.EmailAddress
                })
                .ToList()
        };
        await PopulateEditOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Reservation.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReservationEditViewModel viewModel)
    {
        viewModel.Id = id;
        if (!ModelState.IsValid)
        {
            await PopulateEditOptionsAsync(viewModel);
            return View(viewModel);
        }

        try
        {
            await reservationService.UpdateAsync(viewModel.ToInput(
                currentUserContext,
                GetAccessScope(),
                User.HasPermission(PermissionCatalog.Reservation.OverrideTimeRestriction)));
            return RedirectToAction(nameof(Details), new { id = id.ToHashId() });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateEditOptionsAsync(viewModel);
            return View(viewModel);
        }
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
            var createAndApprove = viewModel.CreateAndApprove
                && User.HasPermission(PermissionCatalog.Reservation.Approve);
            await reservationService.CreateRequestAsync(
                viewModel.ToInput(
                    currentUserContext,
                    GetAccessScope(),
                    createAndApprove));

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
            GetAccessScope(),
            User.HasPermission(PermissionCatalog.Reservation.OverrideTimeRestriction),
            currentUserContext.UserId));

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

    private async Task PopulateEditOptionsAsync(ReservationEditViewModel viewModel)
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
