using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KiraTakip.Services;

public class YetkiKapsamiCacheService : IYetkiKapsamiCache
{
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public YetkiKapsamiCacheService(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<KullaniciKapsamDto> GetAsync(string userId)
    {
        if (_cache.TryGetValue(CacheKey(userId), out KullaniciKapsamDto? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.TumTasinmazlaraErisim })
            .FirstOrDefaultAsync();

        if (user == null)
            return new KullaniciKapsamDto { GlobalErisim = false };

        bool isGlobal = user.TumTasinmazlaraErisim;
        if (!isGlobal)
        {
            isGlobal = await db.UserRoller
                .Where(ur => ur.UserId == userId)
                .Join(db.Roller, ur => ur.RolId, r => r.Id, (ur, r) => r.Ad)
                .AnyAsync(ad => ad == RoleNames.SistemYoneticisi);
        }

        KullaniciKapsamDto dto;
        if (isGlobal)
        {
            dto = new KullaniciKapsamDto { GlobalErisim = true };
        }
        else
        {
            var tasinmazIds = await db.KullaniciYetkiKapsamlari
                .Where(k => k.UserId == userId && k.KapsamTipi == KapsamTipi.Tasinmaz)
                .Select(k => k.KapsamId)
                .ToListAsync();
            dto = new KullaniciKapsamDto { GlobalErisim = false, TasinmazIds = tasinmazIds };
        }

        _cache.Set(CacheKey(userId), dto, Ttl);
        return dto;
    }

    public void Invalidate(string userId) => _cache.Remove(CacheKey(userId));

    public void InvalidateMany(IEnumerable<string> userIds)
    {
        foreach (var id in userIds)
            _cache.Remove(CacheKey(id));
    }

    private static string CacheKey(string userId) => $"YetkiKapsami_{userId}";
}
