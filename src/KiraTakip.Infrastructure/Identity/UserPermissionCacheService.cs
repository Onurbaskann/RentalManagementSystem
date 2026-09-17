using System.Collections.Frozen;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Services.Identity;

public class UserPermissionCacheService(
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory) : IUserPermissionCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly object[] _lockStripes = Enumerable.Range(0, 128).Select(_ => new object()).ToArray();

    public async Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Kullanıcı ID boş veya geçersiz olamaz.", nameof(userId));

        var cacheKey = CacheKey(userId);
        if (cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached != null)
            return cached;

        Guid expectedVersion;
        lock (GetLock(userId))
        {
            if (cache.TryGetValue(cacheKey, out cached) && cached != null)
                return cached;

            if (!cache.TryGetValue(VersionKey(userId), out expectedVersion))
            {
                expectedVersion = Guid.NewGuid();
                cache.Set(VersionKey(userId), expectedVersion, Ttl);
            }
        }

        using var scope = scopeFactory.CreateScope();
        var userRoleRepository = scope.ServiceProvider.GetRequiredService<IUserRoleRepository>();
        var permissions = await userRoleRepository.GetPermissionsAsync(userId, cancellationToken);
        var result = permissions.ToFrozenSet(StringComparer.Ordinal);

        lock (GetLock(userId))
        {
            if (cache.TryGetValue(VersionKey(userId), out Guid currentVersion) && currentVersion == expectedVersion)
            {
                cache.Set(cacheKey, result, Ttl);
            }
        }

        return result;
    }

    public void Invalidate(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Kullanıcı ID boş veya geçersiz olamaz.", nameof(userId));

        lock (GetLock(userId))
        {
            cache.Remove(CacheKey(userId));
            cache.Set(VersionKey(userId), Guid.NewGuid(), Ttl);
        }
    }

    public void InvalidateMany(IEnumerable<string> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var uniqueIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal);

        foreach (var userId in uniqueIds)
        {
            Invalidate(userId);
        }
    }

    private object GetLock(string userId)
    {
        var hash = (uint)StringComparer.Ordinal.GetHashCode(userId);
        return _lockStripes[hash % _lockStripes.Length];
    }

    private static string CacheKey(string userId) => $"KullaniciYetkileri_{userId}";
    private static string VersionKey(string userId) => $"KullaniciYetkileri_Ver_{userId}";
}
