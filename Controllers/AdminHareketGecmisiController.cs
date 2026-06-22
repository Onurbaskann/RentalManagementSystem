using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.Audit.View)]
[Route("Admin/HareketGecmisi")]
public class AdminHareketGecmisiController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminHareketGecmisiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterViewModel filter)
    {
        filter.Sayfa = Math.Max(1, filter.Sayfa);

        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.EventType))
            query = query.Where(a => a.EventType == filter.EventType);

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.BaslangicTarihi.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.BaslangicTarihi.Value.ToUniversalTime());

        if (filter.BitisTarihi.HasValue)
            query = query.Where(a => a.CreatedAt < filter.BitisTarihi.Value.AddDays(1).ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(filter.KullaniciEmail))
        {
            var user = await _userManager.FindByEmailAsync(filter.KullaniciEmail);
            if (user != null)
            {
                query = query.Where(a => a.UserId == user.Id);
            }
            else
            {
                query = query.Where(_ => false);
                filter.KullaniciBulunamadiMesaji = $"\"{filter.KullaniciEmail}\" adresine sahip bir kullanıcı bulunamadı.";
            }
        }

        filter.ToplamKayit = await query.CountAsync();
        filter.MevcutEventTypes = await _db.AuditLogs.Select(a => a.EventType).Distinct().OrderBy(e => e).ToListAsync();
        filter.MevcutEntityTypes = await _db.AuditLogs.Where(a => a.EntityType != null).Select(a => a.EntityType!).Distinct().OrderBy(e => e).ToListAsync();

        var kayitlar = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Sayfa - 1) * AuditLogFilterViewModel.SayfaBoyutu)
            .Take(AuditLogFilterViewModel.SayfaBoyutu)
            .ToListAsync();

        var userIds = kayitlar.Where(k => k.UserId != null).Select(k => k.UserId!).Distinct().ToList();
        var userMap = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.AdSoyad, u.Email })
            .ToDictionaryAsync(u => u.Id);

        filter.Kayitlar = kayitlar.Select(k => new AuditLogSatirViewModel
        {
            Id = k.Id,
            EventType = k.EventType,
            EntityType = k.EntityType,
            EntityId = k.EntityId,
            KullaniciAdSoyad = k.UserId != null && userMap.TryGetValue(k.UserId, out var u)
                ? (u.AdSoyad ?? u.Email ?? k.UserId)
                : k.UserId,
            IpAddress = k.IpAddress,
            Details = k.Details,
            CreatedAt = k.CreatedAt
        }).ToList();

        return View(filter);
    }
}
