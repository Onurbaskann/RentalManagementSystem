using Microsoft.Extensions.DependencyInjection;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Spaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace KiraTakip.Services.Identity;

public class PermissionScopeCacheService(
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory) : IPermissionScopeCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private CancellationTokenSource _resetToken = new();

    public async Task<UserScopeDto> GetAsync(string userId)
    {
        if (cache.TryGetValue(CacheKey(userId), out UserScopeDto? cached) && cached != null)
            return cached;

        using var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        var permissionScopeRepository = scope.ServiceProvider
            .GetRequiredService<IUserPermissionScopeRepository>();
        var spaceScopeService = scope.ServiceProvider.GetRequiredService<ISpaceScopeService>();
        var user = await userRepository.GetScopeAccountAsync(userId);
        if (user == null)
            return new UserScopeDto { GlobalAccess = false };

        var spaceIds = user.HasGlobalAccess
            ? []
            : await permissionScopeRepository.GetScopeIdsAsync(userId, ScopeType.Space);
        var spaceScope = await spaceScopeService.BuildAsync(user.HasGlobalAccess, spaceIds);
        var dto = new UserScopeDto
        {
            GlobalAccess = spaceScope.HasGlobalAccess,
            SpaceRootPaths = spaceScope.RootPaths
        };

        cache.Set(
            CacheKey(userId),
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(Ttl)
                .AddExpirationToken(new CancellationChangeToken(_resetToken.Token)));
        return dto;
    }

    public void Invalidate(string userId) => cache.Remove(CacheKey(userId));

    public void InvalidateMany(IEnumerable<string> userIds)
    {
        foreach (var userId in userIds)
            cache.Remove(CacheKey(userId));
    }

    public void InvalidateAll()
    {
        var previous = Interlocked.Exchange(ref _resetToken, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }

    private static string CacheKey(string userId) => $"YetkiKapsami_{userId}";
}
