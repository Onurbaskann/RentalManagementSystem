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

    public ChargeController(IChargeService tahakkukService, ApplicationDbContext ctx, IPermissionScopeProvider provider)
    {
        _chargeService = tahakkukService;
        _ctx = ctx;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _chargeService.GecikmeleriGuncelleAsync();

        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        var birimIds = (!_provider.GlobalErisim && _provider.ErisilebilirBirimIds.Count > 0)
            ? _provider.ErisilebilirBirimIds : null;
        var pagedResult = await _chargeService.GetPagedAsync(query, tasinmazIds: tasinmazIds, birimIds: birimIds);

        if (!_provider.GlobalErisim)
        {
            if (birimIds != null)
            {
                var birimIdList = birimIds.ToList();
                ViewBag.Properties = await _ctx.Units
                    .Where(b => birimIdList.Contains(b.Id))
                    .Select(b => b.Property).Distinct().OrderBy(t => t.Name).ToListAsync();
                ViewBag.Units = await _ctx.Units
                    .Where(b => birimIdList.Contains(b.Id)).OrderBy(b => b.PropertyId).ThenBy(b => b.Name).ToListAsync();
                var sozKiraciIds2 = await _ctx.Leases
                    .Where(s => birimIdList.Contains(s.UnitId)).Select(s => s.TenantId).Distinct().ToListAsync();
                ViewBag.Kiracilar = await _ctx.Tenants
                    .Where(k => sozKiraciIds2.Contains(k.Id)).OrderBy(k => k.Name).ToListAsync();
            }
            else
            {
                ViewBag.Properties = await _ctx.Properties
                    .Where(t => tasinmazIds!.Contains(t.Id)).OrderBy(t => t.Name).ToListAsync();
                ViewBag.Units = await _ctx.Units
                    .Where(b => tasinmazIds!.Contains(b.PropertyId)).OrderBy(b => b.PropertyId).ThenBy(b => b.Name).ToListAsync();
                var sozKiraciIds = await _ctx.Leases
                    .Where(s => tasinmazIds!.Contains(s.Unit.PropertyId)).Select(s => s.TenantId).Distinct().ToListAsync();
                ViewBag.Kiracilar = await _ctx.Tenants
                    .Where(k => sozKiraciIds.Contains(k.Id)).OrderBy(k => k.Name).ToListAsync();
            }
        }
        else
        {
            ViewBag.Properties = await _ctx.Properties.OrderBy(t => t.Name).ToListAsync();
            ViewBag.Units = await _ctx.Units.OrderBy(b => b.PropertyId).ThenBy(b => b.Name).ToListAsync();
            ViewBag.Kiracilar = await _ctx.Tenants.OrderBy(k => k.Name).ToListAsync();
        }
        ViewBag.MevcutYillar = await _ctx.Charges
            .Select(t => t.PeriodStart.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(query.Durum) || query.Durum == "tum")
        {
            var iptalQuery = _ctx.Charges.Where(t => t.Status == ChargeStatus.Cancelled);
            if (tasinmazIds != null)
                iptalQuery = iptalQuery.Where(t => tasinmazIds.Contains(t.Unit.PropertyId));
            ViewBag.IptalEdildiSayisi = await iptalQuery.CountAsync();
        }
        else
        {
            ViewBag.IptalEdildiSayisi = 0;
        }

        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(pagedResult);
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var charge = await _chargeService.GetDetayAsync(id);
        if (charge == null) return NotFound();

        if (charge.TasinmazId != null && !_provider.KapsamdaMi(charge.TasinmazId.Value))
            return Forbid();

        return View(charge);
    }
}