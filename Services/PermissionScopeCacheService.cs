using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace KiraTakip.Services;

public class PermissionScopeCacheService(
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory) : IPermissionScopeCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public async Task<UserScopeDto> GetAsync(string userId)
    {
        if (cache.TryGetValue(CacheKey(userId), out UserScopeDto? cached) && cached != null)
            return cached;

        using var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        var permissionScopeRepository = scope.ServiceProvider
            .GetRequiredService<IUserPermissionScopeRepository>();
        var user = await userRepository.GetScopeAccountAsync(userId);
        if (user == null)
            return new UserScopeDto { GlobalAccess = false };

        UserScopeDto dto;
        if (user.HasGlobalAccess)
        {
            dto = new UserScopeDto { GlobalAccess = true };
        }
        else
        {
            dto = new UserScopeDto
            {
                GlobalAccess = false,
                PropertyIds = await permissionScopeRepository.GetScopeIdsAsync(
                    userId,
                    ScopeType.Property),
                UnitIds = await permissionScopeRepository.GetScopeIdsAsync(
                    userId,
                    ScopeType.Unit)
            };
        }

        cache.Set(CacheKey(userId), dto, Ttl);
        return dto;
    }

    public void Invalidate(string userId) => cache.Remove(CacheKey(userId));

    public void InvalidateMany(IEnumerable<string> userIds)
    {
        foreach (var userId in userIds)
            cache.Remove(CacheKey(userId));
    }

    private static string CacheKey(string userId) => $"YetkiKapsami_{userId}";
}