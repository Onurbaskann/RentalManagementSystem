using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KiraTakip.Services;

public class PermissionScopeCacheService : IPermissionScopeCache
{
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public PermissionScopeCacheService(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<UserScopeDto> GetAsync(string userId)
    {
        if (_cache.TryGetValue(CacheKey(userId), out UserScopeDto? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.TumTasinmazlaraErisim, u.IsSuperAdmin })
            .FirstOrDefaultAsync();

        if (user == null)
            return new UserScopeDto { GlobalAccess = false };

        bool isGlobal = user.TumTasinmazlaraErisim || user.IsSuperAdmin;

        UserScopeDto dto;
        if (isGlobal)
        {
            dto = new UserScopeDto { GlobalAccess = true };
        }
        else
        {
            var propertyIds = await db.KullaniciYetkiKapsamlari
                .Where(k => k.UserId == userId && k.ScopeType == ScopeType.Property)
                .Select(k => k.ScopeId)
                .ToListAsync();
            var unitIds = await db.KullaniciYetkiKapsamlari
                .Where(k => k.UserId == userId && k.ScopeType == ScopeType.Unit)
                .Select(k => k.ScopeId)
                .ToListAsync();
            dto = new UserScopeDto { GlobalAccess = false, PropertyIds = propertyIds, UnitIds = unitIds };
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
