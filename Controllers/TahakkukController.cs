using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using KiraTakip.Authorization;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public TahakkukController(ITahakkukService tahakkukService, ApplicationDbContext ctx, UserManager<ApplicationUser> userManager)
    {
        _tahakkukService = tahakkukService;
        _ctx = ctx;
        _userManager = userManager;
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        var userId = User.IsInRole(RoleNames.Goruntuleyici) ? _userManager.GetUserId(User) : null;
        var pagedResult = await _tahakkukService.GetPagedAsync(query, userId: userId);

        if (User.IsInRole(RoleNames.Goruntuleyici))
        {
            var uid = _userManager.GetUserId(User)!;
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == uid).Select(u => u.TasinmazId).ToListAsync();
            ViewBag.Tasinmazlar = await _ctx.Tasinmazlar
                .Where(t => yetkiliIds.Contains(t.Id)).OrderBy(t => t.Ad).ToListAsync();
            ViewBag.Birimler = await _ctx.Birimler
                .Where(b => yetkiliIds.Contains(b.TasinmazId)).OrderBy(b => b.TasinmazId).ThenBy(b => b.Ad).ToListAsync();
            var sozKiraciIds = await _ctx.Sozlesmeler
                .Where(s => yetkiliIds.Contains(s.Birim.TasinmazId)).Select(s => s.KiraciId).Distinct().ToListAsync();
            ViewBag.Kiracilar = await _ctx.Kiraciler
                .Where(k => sozKiraciIds.Contains(k.Id)).OrderBy(k => k.Ad).ToListAsync();
        }
        else
        {
            ViewBag.Tasinmazlar = await _ctx.Tasinmazlar.OrderBy(t => t.Ad).ToListAsync();
            ViewBag.Birimler = await _ctx.Birimler.OrderBy(b => b.TasinmazId).ThenBy(b => b.Ad).ToListAsync();
            ViewBag.Kiracilar = await _ctx.Kiraciler.OrderBy(k => k.Ad).ToListAsync();
        }
        ViewBag.MevcutYillar = await _ctx.KiraTahakkuklar
            .Select(t => t.DonemBaslangic.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(pagedResult);
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> Detay(int id)
    {
        var tahakkuk = await _tahakkukService.GetByIdAsync(id);
        if (tahakkuk == null) return NotFound();

        if (User.IsInRole(RoleNames.Goruntuleyici))
        {
            var userId = _userManager.GetUserId(User);
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            if (!yetkiliIds.Contains(tahakkuk.KiraSozlesmesi.Birim.TasinmazId))
                return Forbid();
        }

        return View(tahakkuk);
    }

}
