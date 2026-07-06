using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize]
public class ChargeController : Controller
{
    private readonly IChargeService _chargeService;
    private readonly ApplicationDbContext _ctx;
    private readonly IPermissionScopeProvider _provider;

    public ChargeController(IChargeService chargeService, ApplicationDbContext ctx, IPermissionScopeProvider provider)
    {
        _chargeService = chargeService;
        _ctx = ctx;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _chargeService.UpdateDelaysAsync();

        var propertyIds = _provider.GlobalAccess ? null : _provider.AccessiblePropertyIds;
        var unitIds = (!_provider.GlobalAccess && _provider.AccessibleUnitIds.Count > 0)
            ? _provider.AccessibleUnitIds : null;
        var pagedResult = await _chargeService.GetPagedAsync(query, propertyIds: propertyIds, unitIds: unitIds);

        if (!_provider.GlobalAccess)
        {
            if (unitIds != null)
            {
                var unitIdList = unitIds.ToList();
                ViewBag.Properties = await _ctx.Units
                    .Where(b => unitIdList.Contains(b.Id))
                    .Select(b => b.Property).Distinct().OrderBy(t => t.Name).ToListAsync();
                ViewBag.Units = await _ctx.Units
                    .Where(b => unitIdList.Contains(b.Id)).OrderBy(b => b.PropertyId).ThenBy(b => b.Name).ToListAsync();
                var leaseTenantIdsByUnit = await _ctx.Leases
                    .Where(s => unitIdList.Contains(s.UnitId)).Select(s => s.TenantId).Distinct().ToListAsync();
                ViewBag.Tenants = await _ctx.Tenants
                    .Where(k => leaseTenantIdsByUnit.Contains(k.Id)).OrderBy(k => k.Name).ToListAsync();
            }
            else
            {
                ViewBag.Properties = await _ctx.Properties
                    .Where(t => propertyIds!.Contains(t.Id)).OrderBy(t => t.Name).ToListAsync();
                ViewBag.Units = await _ctx.Units
                    .Where(b => propertyIds!.Contains(b.PropertyId)).OrderBy(b => b.PropertyId).ThenBy(b => b.Name).ToListAsync();
                var leaseTenantIds = await _ctx.Leases
                    .Where(s => propertyIds!.Contains(s.Unit.PropertyId)).Select(s => s.TenantId).Distinct().ToListAsync();
                ViewBag.Tenants = await _ctx.Tenants
                    .Where(k => leaseTenantIds.Contains(k.Id)).OrderBy(k => k.Name).ToListAsync();
            }
        }
        else
        {
            ViewBag.Properties = await _ctx.Properties.OrderBy(t => t.Name).ToListAsync();
            ViewBag.Units = await _ctx.Units.OrderBy(b => b.PropertyId).ThenBy(b => b.Name).ToListAsync();
            ViewBag.Tenants = await _ctx.Tenants.OrderBy(k => k.Name).ToListAsync();
        }
        ViewBag.AvailableYears = await _ctx.Charges
            .Select(t => t.PeriodStart.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(query.Status) || query.Status == "tum")
        {
            var cancelledQuery = _ctx.Charges.Where(t => t.Status == ChargeStatus.Cancelled);
            if (propertyIds != null)
                cancelledQuery = cancelledQuery.Where(t => propertyIds.Contains(t.Unit.PropertyId));
            ViewBag.CancelledCount = await cancelledQuery.CountAsync();
        }
        else
        {
            ViewBag.CancelledCount = 0;
        }

        ViewBag.Query = query;
        ViewBag.Status = query.Status ?? "tum";
        return View(pagedResult);
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var charge = await _chargeService.GetDetailsAsync(id);
        if (charge == null) return NotFound();

        if (charge.PropertyId != null && !_provider.IsInScope(charge.PropertyId.Value))
            return Forbid();

        return View(charge);
    }
}