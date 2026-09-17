using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KiraTakip.Tests;

public class PermissionAuthorizationHandlerTests
{
    private sealed class DynamicPermissionCache : IUserPermissionCache
    {
        private readonly Dictionary<string, HashSet<string>> _store = new(StringComparer.Ordinal);

        public void SetUserPermissions(string userId, params string[] permissions)
        {
            _store[userId] = new HashSet<string>(permissions, StringComparer.Ordinal);
        }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(userId, out var perms))
            {
                return Task.FromResult<IReadOnlySet<string>>(perms);
            }

            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        }

        public void Invalidate(string userId) => _store.Remove(userId);
        public void InvalidateMany(IEnumerable<string> userIds)
        {
            foreach (var id in userIds) _store.Remove(id);
        }
    }

    private static (IAuthorizationService AuthService, DynamicPermissionCache Cache, DefaultHttpContext HttpContext) CreateAuthorizationChain(ClaimsPrincipal? requestUser = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ApplicationDbContext>(_ => null!);
        services.AddScoped<IUserRoleService>(_ => null!);

        var cache = new DynamicPermissionCache();
        services.AddSingleton<IUserPermissionCache>(cache);

        var httpContext = new DefaultHttpContext();
        if (requestUser != null)
        {
            httpContext.User = requestUser;
        }
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

        services.AddIdentityModule();

        var sp = services.BuildServiceProvider();
        var authService = sp.GetRequiredService<IAuthorizationService>();
        return (authService, cache, httpContext);
    }

    [Fact]
    public async Task Internal_ValidId_IsSuperAdminTrue_SucceedsForValidPermission()
    {
        var superAdmin = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth, _, _) = CreateAuthorizationChain(superAdmin);
        var result = await auth.AuthorizeAsync(superAdmin, PermissionCatalog.Property.Edit);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Tenant_IsSuperAdminTrue_NoCachePermissions_AccessDenied()
    {
        var tenantUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "tenant-1"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Tenant).ToString()),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth, _, _) = CreateAuthorizationChain(tenantUser);
        var result = await auth.AuthorizeAsync(tenantUser, PermissionCatalog.Property.Edit);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task MissingOrInvalidUserType_SuperAdminExceptionDoesNotApply()
    {
        var missingUserType = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-2"),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth1, _, _) = CreateAuthorizationChain(missingUserType);
        var res1 = await auth1.AuthorizeAsync(missingUserType, PermissionCatalog.Property.Edit);
        Assert.False(res1.Succeeded);

        var invalidUserType = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-3"),
            new Claim(AppClaimTypes.UserType, "not-a-number"),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth2, _, _) = CreateAuthorizationChain(invalidUserType);
        var res2 = await auth2.AuthorizeAsync(invalidUserType, PermissionCatalog.Property.Edit);
        Assert.False(res2.Succeeded);
    }

    [Fact]
    public async Task MissingOrEmptyNameIdentifier_AccessDenied()
    {
        var noIdUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth1, _, _) = CreateAuthorizationChain(noIdUser);
        var res1 = await auth1.AuthorizeAsync(noIdUser, PermissionCatalog.Property.Edit);
        Assert.False(res1.Succeeded);

        var emptyIdUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "   "),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth2, _, _) = CreateAuthorizationChain(emptyIdUser);
        var res2 = await auth2.AuthorizeAsync(emptyIdUser, PermissionCatalog.Property.Edit);
        Assert.False(res2.Succeeded);
    }

    [Fact]
    public async Task AnonymousUser_AccessDenied()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var (auth, _, _) = CreateAuthorizationChain(anonymous);
        var result = await auth.AuthorizeAsync(anonymous, PermissionCatalog.Property.Edit);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PermissionInCookie_NotInCache_AccessDenied()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "cookie-user"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Module),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Edit)
        }, "Cookies"));

        var (auth, _, _) = CreateAuthorizationChain(user);
        // Cache is completely empty for cookie-user
        var result = await auth.AuthorizeAsync(user, PermissionCatalog.Property.Edit);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task NoPermissionInCookie_ModuleAndActionInCache_AccessGranted()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "clean-cookie-user"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        }, "Cookies"));

        var (auth, cache, _) = CreateAuthorizationChain(user);
        cache.SetUserPermissions("clean-cookie-user", PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);

        var result = await auth.AuthorizeAsync(user, PermissionCatalog.Property.Edit);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task OnlyActionInCache_NoModule_AccessDenied()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "action-only-user"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        }, "Cookies"));

        var (auth, cache, _) = CreateAuthorizationChain(user);
        cache.SetUserPermissions("action-only-user", PermissionCatalog.Property.Edit);

        var result = await auth.AuthorizeAsync(user, PermissionCatalog.Property.Edit);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UnknownPermissionOrModule_AccessDeniedEvenForSuperAdmin()
    {
        var superAdmin = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "super-admin-1"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "true")
        }, "Cookies"));

        var (auth, _, _) = CreateAuthorizationChain(superAdmin);

        var unknownReq = new PermissionRequirement("Unknown.Permission.Name");
        var result = await auth.AuthorizeAsync(superAdmin, null, new[] { unknownReq });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task DifferentRequestPrincipalVsTargetPrincipal_EvaluatesTargetUserPermissions()
    {
        var requestUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "request-user-A"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        }, "Cookies"));

        var targetUserB = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "target-user-B"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        }, "Cookies"));

        var (auth, cache, _) = CreateAuthorizationChain(requestUser);
        cache.SetUserPermissions("target-user-B", PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);

        // When target is UserB (even though request is UserA)
        var resultB = await auth.AuthorizeAsync(targetUserB, PermissionCatalog.Property.Edit);
        Assert.True(resultB.Succeeded);

        // When target is UserA (request user who has no permissions in cache)
        var resultA = await auth.AuthorizeAsync(requestUser, PermissionCatalog.Property.Edit);
        Assert.False(resultA.Succeeded);
    }
}