using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.RateSchedule;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Common;

namespace KiraTakip.Controllers;

[Route("Admin/Rate")]
public class AdminRateController(IRateScheduleService rateScheduleService) : Controller
{
    [Authorize(Policy = PermissionCatalog.RateSchedule.Module)]
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var summaries = await rateScheduleService.GetYearSummariesPagedAsync(query);
        ViewBag.Query = query;
        return View(summaries);
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Module)]
    [HttpGet("Year/{year:int}")]
    public async Task<IActionResult> Detail(int year)
    {
        var matrix = await rateScheduleService.GetMatrixAsync(year);
        if (matrix == null) return NotFound();

        return View(ToViewModel(matrix));
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Edit)]
    [HttpPost("Year/{year:int}/UpdateLine")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLine(int year, RateMatrixPostViewModel vm)
    {
        if (year != vm.Year) return BadRequest();

        if (!ModelState.IsValid)
        {
            var matrix = await rateScheduleService.GetMatrixAsync(year);
            if (matrix == null) return NotFound();

            var viewModel = ToViewModel(matrix);
            ApplyPostedValues(viewModel, vm);
            return View(nameof(Detail), viewModel);
        }

        var input = new SaveRateMatrixInput(
            vm.Cells.Select(ce => new SaveRateCellInput(
                ce.LineItemId,
                ce.TenantCategoryId,
                ce.ChargeTypeId,
                ce.CalculationMethod,
                ce.UnitValue,
                ce.KdvRate
            )).ToList(),
            vm.ReservationCells.Select(rc => new SaveReservationCellInput(
                rc.ReservationRateId,
                rc.UnitTypeId,
                rc.FreeDurationMinutes,
                rc.BillingPeriodMinutes,
                rc.PeriodRate,
                rc.KdvRate
            )).ToList()
        );

        await rateScheduleService.SaveMatrixAsync(year, input);

        return RedirectToAction(nameof(Detail), new { year });
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Edit)]
    [HttpGet("AddYear")]
    public async Task<IActionResult> AddYear()
    {
        var existingYears = await rateScheduleService.GetExistingYearsAsync();
        ViewBag.ExistingYears = existingYears;
        return View(new RateYearAddViewModel { Year = DateTime.Now.Year });
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Create)]
    [HttpPost("AddYear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddYear(RateYearAddViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ExistingYears = await rateScheduleService.GetExistingYearsAsync();
            return View(vm);
        }

        try
        {
            await rateScheduleService.CreateYearAsync(new CreateRateYearInput(vm.Year, vm.CopyFromYear));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            ViewBag.ExistingYears = await rateScheduleService.GetExistingYearsAsync();
            return View(vm);
        }

        return RedirectToAction(nameof(Detail), new { year = vm.Year });
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Edit)]
    [HttpPost("ToggleStatus/{year:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int year)
    {
        await rateScheduleService.ToggleStatusAsync(year);
        return RedirectToAction(nameof(Index));
    }

    private static RateMatrixViewModel ToViewModel(RateMatrixDto matrix) => new()
    {
        Year = matrix.Year,
        IsActive = matrix.IsActive,
        Columns = matrix.Columns.Select(column => new RateMatrixChargeTypeColumn
        {
            ChargeTypeId = column.ChargeTypeId,
            ChargeTypeName = column.Name,
            ChargeTypeCode = column.Code
        }).ToList(),
        Rows = matrix.Rows.Select(row => new RateMatrixRow
        {
            TenantCategoryId = row.TenantCategoryId,
            TenantCategoryName = row.TenantCategoryName,
            Cells = row.Cells.Select(cell => new RateMatrixCell
            {
                LineItemId = cell.LineItemId,
                TenantCategoryId = cell.TenantCategoryId,
                ChargeTypeId = cell.ChargeTypeId,
                CalculationMethod = cell.CalculationMethod,
                UnitValue = cell.UnitValue,
                KdvRate = cell.KdvRate
            }).ToList()
        }).ToList(),
        ReservationRows = matrix.ReservationRows.Select(row => new RateMatrixReservationRow
        {
            ReservationRateId = row.ReservationRateId,
            UnitTypeId = row.UnitTypeId,
            UnitTypeName = row.UnitTypeName,
            FreeDurationMinutes = row.FreeDurationMinutes,
            BillingPeriodMinutes = row.BillingPeriodMinutes,
            PeriodRate = row.PeriodRate,
            KdvRate = row.KdvRate
        }).ToList()
    };

    private static void ApplyPostedValues(RateMatrixViewModel matrix, RateMatrixPostViewModel posted)
    {
        var postedCells = posted.Cells
            .GroupBy(cell => (cell.TenantCategoryId, cell.ChargeTypeId))
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (var cell in matrix.Rows.SelectMany(row => row.Cells))
        {
            if (!postedCells.TryGetValue((cell.TenantCategoryId, cell.ChargeTypeId), out var postedCell))
                continue;

            cell.CalculationMethod = postedCell.CalculationMethod;
            cell.UnitValue = postedCell.UnitValue;
            cell.KdvRate = postedCell.KdvRate;
        }

        var postedReservationCells = posted.ReservationCells
            .GroupBy(cell => cell.UnitTypeId)
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (var cell in matrix.ReservationRows)
        {
            if (!postedReservationCells.TryGetValue(cell.UnitTypeId, out var postedCell))
                continue;

            cell.FreeDurationMinutes = postedCell.FreeDurationMinutes;
            cell.BillingPeriodMinutes = postedCell.BillingPeriodMinutes;
            cell.PeriodRate = postedCell.PeriodRate;
            cell.KdvRate = postedCell.KdvRate;
        }
    }
}
