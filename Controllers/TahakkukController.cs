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
public class TahakkukController : Controller
{
    private readonly ITahakkukService _tahakkukService;
    private readonly ApplicationDbContext _ctx;
    private readonly IYetkiKapsamiProvider _provider;

    public TahakkukController(ITahakkukService tahakkukService, ApplicationDbContext ctx, IYetkiKapsamiProvider provider)
    {
        _tahakkukService = tahakkukService;
        _ctx = ctx;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Odeme.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        var birimIds = (!_provider.GlobalErisim && _provider.ErisilebilirBirimIds.Count > 0)
            ? _provider.ErisilebilirBirimIds : null;
        var pagedResult = await _tahakkukService.GetPagedAsync(query, tasinmazIds: tasinmazIds, birimIds: birimIds);

        if (!_provider.GlobalErisim)
        {
            if (birimIds != null)
            {
                var birimIdList = birimIds.ToList();
                ViewBag.Tasinmazlar = await _ctx.Birimler
                    .Where(b => birimIdList.Contains(b.Id))
                    .Select(b => b.Tasinmaz).Distinct().OrderBy(t => t.Ad).ToListAsync();
                ViewBag.Birimler = await _ctx.Birimler
                    .Where(b => birimIdList.Contains(b.Id)).OrderBy(b => b.TasinmazId).ThenBy(b => b.Ad).ToListAsync();
                var sozKiraciIds2 = await _ctx.Sozlesmeler
                    .Where(s => birimIdList.Contains(s.BirimId)).Select(s => s.KiraciId).Distinct().ToListAsync();
                ViewBag.Kiracilar = await _ctx.Kiraciler
                    .Where(k => sozKiraciIds2.Contains(k.Id)).OrderBy(k => k.Ad).ToListAsync();
            }
            else
            {
                ViewBag.Tasinmazlar = await _ctx.Tasinmazlar
                    .Where(t => tasinmazIds!.Contains(t.Id)).OrderBy(t => t.Ad).ToListAsync();
                ViewBag.Birimler = await _ctx.Birimler
                    .Where(b => tasinmazIds!.Contains(b.TasinmazId)).OrderBy(b => b.TasinmazId).ThenBy(b => b.Ad).ToListAsync();
                var sozKiraciIds = await _ctx.Sozlesmeler
                    .Where(s => tasinmazIds!.Contains(s.Birim.TasinmazId)).Select(s => s.KiraciId).Distinct().ToListAsync();
                ViewBag.Kiracilar = await _ctx.Kiraciler
                    .Where(k => sozKiraciIds.Contains(k.Id)).OrderBy(k => k.Ad).ToListAsync();
            }
        }
        else
        {
            ViewBag.Tasinmazlar = await _ctx.Tasinmazlar.OrderBy(t => t.Ad).ToListAsync();
            ViewBag.Birimler = await _ctx.Birimler.OrderBy(b => b.TasinmazId).ThenBy(b => b.Ad).ToListAsync();
            ViewBag.Kiracilar = await _ctx.Kiraciler.OrderBy(k => k.Ad).ToListAsync();
        }
        ViewBag.MevcutYillar = await _ctx.Tahakkuklar
            .Select(t => t.DonemBaslangic.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(query.Durum) || query.Durum == "tum")
        {
            var iptalQuery = _ctx.Tahakkuklar.Where(t => t.Durum == TahakkukDurumu.IptalEdildi);
            if (tasinmazIds != null)
                iptalQuery = iptalQuery.Where(t => tasinmazIds.Contains(t.Birim.TasinmazId));
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

    [Authorize(Policy = PermissionCatalog.Odeme.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var tahakkuk = await _tahakkukService.GetDetayAsync(id);
        if (tahakkuk == null) return NotFound();

        if (tahakkuk.TasinmazId != null && !_provider.KapsamdaMi(tahakkuk.TasinmazId.Value))
            return Forbid();

        return View(tahakkuk);
    }
}