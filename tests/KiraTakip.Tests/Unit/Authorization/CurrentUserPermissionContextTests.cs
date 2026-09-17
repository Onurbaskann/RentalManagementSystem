using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace KiraTakip.Tests;

public class CurrentUserPermissionContextTests
{
    private sealed class TrackingPermissionCache : IUserPermissionCache
    {
        private readonly Dictionary<string, HashSet<string>> _store = new(StringComparer.Ordinal);
        public int GetAsyncCallCount { get; private set; }
        public List<string> RequestedUserIds { get; } = new();

        public void SetUserPermissions(string userId, params string[] permissions)
        {
            _store[userId] = new HashSet<string>(permissions, StringComparer.Ordinal);
        }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            GetAsyncCallCount++;
            RequestedUserIds.Add(userId);

            if (_store.TryGetValue(userId, out var perms))
            {
                return Task.FromResult<IReadOnlySet<string>>(perms);
            }

            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        }

        public void Invalidate(string userId) { }
        public void InvalidateMany(IEnumerable<string> userIds) { }
    }

    [Fact]
    public async Task GetPermissionsAsync_UnauthenticatedUser_ReturnsEmptySet()
    {
        var cache = new TrackingPermissionCache();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()) // IsAuthenticated = false
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserPermissionContext(cache, accessor);

        var permissions = await context.GetPermissionsAsync();

        Assert.Empty(permissions);
        Assert.Equal(0, cache.GetAsyncCallCount);
    }

    [Fact]
    public async Task GetPermissionsAsync_NoNameIdentifier_ReturnsEmptySet()
    {
        var cache = new TrackingPermissionCache();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Alice") }, "TestAuth"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserPermissionContext(cache, accessor);

        var permissions = await context.GetPermissionsAsync();

        Assert.Empty(permissions);
        Assert.Equal(0, cache.GetAsyncCallCount);
    }

    [Fact]
    public async Task GetPermissionsAsync_MultipleCalls_ExecutesOnlySingleTaskPerRequest()
    {
        var cache = new TrackingPermissionCache();
        cache.SetUserPermissions("user-42", PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-42")
            }, "TestAuth"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserPermissionContext(cache, accessor);

        // Multiple concurrent and sequential calls
        var task1 = context.GetPermissionsAsync();
        var task2 = context.GetPermissionsAsync();
        var task3 = context.GetPermissionsAsync();

        var results = await Task.WhenAll(task1, task2, task3);

        Assert.Equal(1, cache.GetAsyncCallCount);
        Assert.Same(results[0], results[1]);
        Assert.Same(results[1], results[2]);
        Assert.Contains(PermissionCatalog.Property.Edit, results[0]);
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_DifferentUserIds_MaintainsIsolationAndFetchesForRequestedUser()
    {
        var cache = new TrackingPermissionCache();
        cache.SetUserPermissions("user-A", PermissionCatalog.Property.Module);
        cache.SetUserPermissions("user-B", PermissionCatalog.Tenant.Module, PermissionCatalog.Tenant.Edit);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-A")
            }, "TestAuth"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserPermissionContext(cache, accessor);

        var permsA = await context.GetPermissionsAsync();
        var permsB = await context.GetPermissionsForUserAsync("user-B");

        Assert.Contains(PermissionCatalog.Property.Module, permsA);
        Assert.DoesNotContain(PermissionCatalog.Tenant.Module, permsA);

        Assert.Contains(PermissionCatalog.Tenant.Module, permsB);
        Assert.DoesNotContain(PermissionCatalog.Property.Module, permsB);

        Assert.Equal(2, cache.GetAsyncCallCount);
        Assert.Equal(new[] { "user-A", "user-B" }, cache.RequestedUserIds);
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_ConcurrentCalls_ShareSingleTask()
    {
        var tcs = new TaskCompletionSource<IReadOnlySet<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;

        var mockCache = new DelegatingPermissionCache((userId, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return tcs.Task;
        });

        var context = new CurrentUserPermissionContext(mockCache, new HttpContextAccessor());

        // Start 10 parallel calls
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => context.GetPermissionsForUserAsync("user-conc")))
            .ToArray();

        // Release the task
        var expectedPerms = new HashSet<string> { PermissionCatalog.Property.Edit };
        tcs.SetResult(expectedPerms);

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, callCount);
        for (int i = 0; i < results.Length; i++)
        {
            Assert.Same(expectedPerms, results[i]);
        }
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_Sequence_A_B_A_OnlyLoadsEachUserOnce()
    {
        var cache = new TrackingPermissionCache();
        cache.SetUserPermissions("user-A", PermissionCatalog.Property.Module);
        cache.SetUserPermissions("user-B", PermissionCatalog.Tenant.Module);

        var context = new CurrentUserPermissionContext(cache, new HttpContextAccessor());

        var permsA1 = await context.GetPermissionsForUserAsync("user-A");
        var permsB = await context.GetPermissionsForUserAsync("user-B");
        var permsA2 = await context.GetPermissionsForUserAsync("user-A");

        Assert.Equal(2, cache.GetAsyncCallCount);
        Assert.Equal(new[] { "user-A", "user-B" }, cache.RequestedUserIds);
        Assert.Same(permsA1, permsA2);
        Assert.Contains(PermissionCatalog.Property.Module, permsA1);
        Assert.Contains(PermissionCatalog.Tenant.Module, permsB);
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_DifferentScopes_CreateSeparateContexts()
    {
        var cache = new TrackingPermissionCache();
        cache.SetUserPermissions("user-1", PermissionCatalog.Property.Module);

        var contextScope1 = new CurrentUserPermissionContext(cache, new HttpContextAccessor());
        var contextScope2 = new CurrentUserPermissionContext(cache, new HttpContextAccessor());

        var perms1 = await contextScope1.GetPermissionsForUserAsync("user-1");
        var perms2 = await contextScope2.GetPermissionsForUserAsync("user-1");

        Assert.Equal(2, cache.GetAsyncCallCount);
        Assert.Contains(PermissionCatalog.Property.Module, perms1);
        Assert.Contains(PermissionCatalog.Property.Module, perms2);
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_CacheException_PropagatesToCallerAndDoesNotGrantAccess()
    {
        var throwingCache = new DelegatingPermissionCache((userId, ct) =>
            throw new InvalidOperationException("Cache database connection failed"));

        var context = new CurrentUserPermissionContext(throwingCache, new HttpContextAccessor());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.GetPermissionsForUserAsync("user-error"));
        Assert.Equal("Cache database connection failed", ex.Message);

        var service = new CurrentUserPermissionService(context, new HttpContextAccessor());
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-error"),
            new Claim(AppClaimTypes.UserType, ((int)KiraTakip.Models.Enums.UserType.Internal).ToString())
        }, "TestAuth"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.HasPermissionAsync(user, PermissionCatalog.Property.Edit));
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_DifferentTargetPrincipal_DoesNotUseCurrentRequestUserPermissions()
    {
        var cache = new TrackingPermissionCache();
        cache.SetUserPermissions("request-user", PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        cache.SetUserPermissions("target-user", PermissionCatalog.Tenant.Module, PermissionCatalog.Tenant.Edit);

        var requestUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "request-user"),
            new Claim(AppClaimTypes.UserType, ((int)KiraTakip.Models.Enums.UserType.Internal).ToString())
        }, "TestAuth"));

        var targetUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "target-user"),
            new Claim(AppClaimTypes.UserType, ((int)KiraTakip.Models.Enums.UserType.Internal).ToString())
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = requestUser };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserPermissionContext(cache, accessor);
        var service = new CurrentUserPermissionService(context, accessor);

        // Request user has Property.Edit, target user does NOT
        var targetHasPropertyEdit = await service.HasPermissionAsync(targetUser, PermissionCatalog.Property.Edit);
        Assert.False(targetHasPropertyEdit);

        // Target user has Tenant.Edit, request user does NOT
        var requestHasTenantEdit = await service.HasPermissionAsync(requestUser, PermissionCatalog.Tenant.Edit);
        Assert.False(requestHasTenantEdit);

        var targetHasTenantEdit = await service.HasPermissionAsync(targetUser, PermissionCatalog.Tenant.Edit);
        Assert.True(targetHasTenantEdit);
    }

    private sealed class DelegatingPermissionCache : IUserPermissionCache
    {
        private readonly Func<string, CancellationToken, Task<IReadOnlySet<string>>> _getter;

        public DelegatingPermissionCache(Func<string, CancellationToken, Task<IReadOnlySet<string>>> getter)
        {
            _getter = getter;
        }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
            => _getter(userId, cancellationToken);

        public void Invalidate(string userId) { }
        public void InvalidateMany(IEnumerable<string> userIds) { }
    }
}