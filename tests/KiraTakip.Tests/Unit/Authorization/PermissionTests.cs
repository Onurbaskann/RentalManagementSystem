using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Razor.TagHelpers;
using KiraTakip.Web.TagHelpers;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.Extensions;
using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.Identity;
using KiraTakip.Services;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Data;
using KiraTakip.Web.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class PermissionTests
{
    // ── AdminBypassHandler ────────────────────────────────────────────────────

    private class DummyRequirement : IAuthorizationRequirement { }

    [Fact]
    public async Task AdminBypassHandler_IsSuperAdmin_TumGereksinimlerSucceedOlur()
    {
        var requirements = new List<IAuthorizationRequirement> { new DummyRequirement(), new DummyRequirement() };
        var principal = CreateSuperAdmin();
        var ctx = new AuthorizationHandlerContext(requirements, principal, null);

        await new AdminBypassHandler().HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task AdminBypassHandler_UnauthenticatedSuperAdmin_GereksinimlerKalir()
    {
        var requirements = new List<IAuthorizationRequirement> { new DummyRequirement() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("IsSuperAdmin", "true")]));
        var ctx = new AuthorizationHandlerContext(requirements, principal, null);

        await new AdminBypassHandler().HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task AdminBypassHandler_IsSuperAdminYok_GereksinimlerKalir()
    {
        var requirements = new List<IAuthorizationRequirement> { new DummyRequirement() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "Yonetici")]));
        var ctx = new AuthorizationHandlerContext(requirements, principal, null);

        await new AdminBypassHandler().HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task AdminBypassHandler_IsSuperAdminYanlisValue_GereksinimlerKalir()
    {
        var requirements = new List<IAuthorizationRequirement> { new DummyRequirement() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("IsSuperAdmin", "false")]));
        var ctx = new AuthorizationHandlerContext(requirements, principal, null);

        await new AdminBypassHandler().HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    // ── PermissionCatalog bütünlük ────────────────────────────────────────────

    [Fact]
    public void PermissionCatalog_AllModules_PathlerBenzersiz()
    {
        var tumPathler = PermissionCatalog.AllModules
            .SelectMany(m => new[] { m.Path }.Concat(m.Actions))
            .ToList();

        var tekrarlar = tumPathler.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.Empty(tekrarlar);
    }

    [Fact]
    public void PermissionCatalog_AllModules_ActionPathlar_ModulePathIleBaslar()
    {
        var hatalar = new List<string>();
        foreach (var module in PermissionCatalog.AllModules)
        {
            foreach (var action in module.Actions)
            {
                if (!action.StartsWith(module.Path + "."))
                    hatalar.Add($"{action} -> beklenen prefix: {module.Path}.");
            }
        }

        Assert.Empty(hatalar);
    }

    [Fact]
    public void PermissionCatalog_AllModules_ViewVeManageKalintisiYok()
    {
        var tumPathler = PermissionCatalog.AllModules
            .SelectMany(m => new[] { m.Path }.Concat(m.Actions));

        var yasakliSonekler = new[] { ".View", ".Manage" };
        var ihlaller = tumPathler
            .Where(p => yasakliSonekler.Any(s => p.EndsWith(s)))
            .ToList();

        Assert.Empty(ihlaller);
    }

    [Fact]
    public void PermissionCatalog_KiraciPortal_DogruPrefixIleKayitli()
    {
        var kiraciModuller = PermissionCatalog.AllModules
            .Where(m => m.Path.StartsWith("Tenant.") || m.Path == "Tenant")
            .ToList();

        Assert.NotEmpty(kiraciModuller);

        foreach (var m in kiraciModuller)
        {
            Assert.True(
                m.Path.StartsWith("Tenant."),
                $"Kiracı portal modülü yanlış prefix: {m.Path}");
        }
    }

    [Fact]
    public void PermissionCatalog_KiraciSystem_DogruPrefixIleKayitli()
    {
        var kiraciSystemModuller = PermissionCatalog.AllModules
            .Where(m => m.Path.StartsWith("Tenant.System."))
            .ToList();

        Assert.NotEmpty(kiraciSystemModuller);

        var beklenenler = new[]
        {
            "Tenant.System.User",
            "Tenant.System.Role"
        };

        foreach (var beklenen in beklenenler)
            Assert.Contains(kiraciSystemModuller, m => m.Path == beklenen);
    }

    [Fact]
    public void PermissionCatalog_AllModules_InternalVeSystemModulleriVar()
    {
        var pathler = PermissionCatalog.AllModules.Select(m => m.Path).ToList();

        Assert.Contains("Internal.Lease", pathler);
        Assert.Contains("Internal.Payment", pathler);
        Assert.Contains("Internal.Tenant", pathler);
        Assert.Contains("Internal.Charge", pathler);
        Assert.Contains("System.User", pathler);
        Assert.Contains("System.Role", pathler);
    }

    [Fact]
    public void PermissionCatalog_LeaseReviewPermissions_AreConsistentAndInternalOnly()
    {
        var expected = new[]
        {
            PermissionCatalog.Lease.Approve,
            PermissionCatalog.Lease.RequestRevision,
            PermissionCatalog.Lease.DeleteDraft
        };

        var leaseModule = Assert.Single(
            PermissionCatalog.AllModules,
            module => module.Path == PermissionCatalog.Lease.Module);
        Assert.All(expected, permission => Assert.Contains(permission, leaseModule.Actions));
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.ScopeAware));
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.InternalAll));
        Assert.All(expected, permission => Assert.DoesNotContain(permission, PermissionCatalog.TenantAll));

        Assert.Equal("Onayla", Assert.Single(leaseModule.ActionDefinitions, x => x.Path == PermissionCatalog.Lease.Approve).DisplayName);
        Assert.Equal("Revizyon İste", Assert.Single(leaseModule.ActionDefinitions, x => x.Path == PermissionCatalog.Lease.RequestRevision).DisplayName);
        Assert.Equal("Başvuru Sil", Assert.Single(leaseModule.ActionDefinitions, x => x.Path == PermissionCatalog.Lease.DeleteDraft).DisplayName);
    }

    [Fact]
    public void PermissionCatalog_ReservationDecisionPermissions_AreConsistentAndInternalOnly()
    {
        var expected = new[]
        {
            PermissionCatalog.Reservation.Approve,
            PermissionCatalog.Reservation.Reject,
            PermissionCatalog.Reservation.OverrideTimeRestriction
        };

        var reservationModule = Assert.Single(
            PermissionCatalog.AllModules,
            module => module.Path == PermissionCatalog.Reservation.Module);
        Assert.All(expected, permission => Assert.Contains(permission, reservationModule.Actions));
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.ScopeAware));
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.InternalAll));
        Assert.All(expected, permission => Assert.DoesNotContain(permission, PermissionCatalog.TenantAll));

        Assert.Equal("Onayla", Assert.Single(
            reservationModule.ActionDefinitions,
            definition => definition.Path == PermissionCatalog.Reservation.Approve).DisplayName);
        Assert.Equal("Reddet", Assert.Single(
            reservationModule.ActionDefinitions,
            definition => definition.Path == PermissionCatalog.Reservation.Reject).DisplayName);
        Assert.Equal("Zaman Sınırı İstisnası", Assert.Single(
            reservationModule.ActionDefinitions,
            definition => definition.Path == PermissionCatalog.Reservation.OverrideTimeRestriction).DisplayName);
    }

    [Fact]
    public void PermissionCatalog_InternalAllAndTenantAll_HaveValidBoundariesAndNoOverlap()
    {
        Assert.DoesNotContain(PermissionCatalog.InternalAll, p => p.StartsWith("Tenant."));
        Assert.All(PermissionCatalog.TenantAll, p => Assert.StartsWith("Tenant.", p));

        var kesisenler = PermissionCatalog.InternalAll.Intersect(PermissionCatalog.TenantAll).ToList();
        Assert.Empty(kesisenler);

        var tenantTekrarlar = PermissionCatalog.TenantAll.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(tenantTekrarlar);

        var internalTekrarlar = PermissionCatalog.InternalAll.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(internalTekrarlar);
    }

    [Fact]
    public void RoleNames_KiraciYoneticisi_Korunmali()
    {
        Assert.Equal("Kiracı Yöneticisi", RoleNames.KiraciYoneticisi);
    }

    [Fact]
    public void CurrentUserContext_ReadsIsSuperAdminOnlyFromTrustedClaim()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                    new Claim("IsSuperAdmin", "true")
                ],
                authenticationType: "Test"))
        };

        var context = new CurrentUserContext(new HttpContextAccessor { HttpContext = httpContext });
        Assert.True(context.IsSuperAdmin);

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "normal-id")],
            authenticationType: "Test"));
        Assert.False(context.IsSuperAdmin);
    }

    // ── PermissionEvaluator & Central Permission Logic ───────────────────────

    private static ClaimsPrincipal CreateUser(params string[] permissions)
    {
        return CreateUserWithId("test-user-id", permissions);
    }

    private static ClaimsPrincipal CreateUserWithId(string userId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        };
        claims.AddRange(permissions.Select(p => new Claim(AppClaimTypes.Permission, p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static ClaimsPrincipal CreateSuperAdmin()
    {
        return new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "super-admin"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "true")
        ], "TestAuth"));
    }

    private sealed class FakeUserPermissionCache : IUserPermissionCache
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _store = new(StringComparer.Ordinal);
        private readonly HashSet<string>? _defaultPermissions;

        public FakeUserPermissionCache(params string[] permissions)
        {
            if (permissions.Length > 0)
            {
                _defaultPermissions = new HashSet<string>(permissions, StringComparer.Ordinal);
            }
        }

        public void SetUserPermissions(string userId, params string[] permissions)
        {
            _store[userId] = new HashSet<string>(permissions, StringComparer.Ordinal);
        }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(userId, out var perms))
                return Task.FromResult<IReadOnlySet<string>>(perms);

            return Task.FromResult<IReadOnlySet<string>>(_defaultPermissions ?? (IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal));
        }

        public void Invalidate(string userId) => _store.TryRemove(userId, out _);
        public void InvalidateMany(IEnumerable<string> userIds)
        {
            foreach (var id in userIds) _store.TryRemove(id, out _);
        }
    }

    private static ICurrentUserPermissionService CreatePermissionService(ClaimsPrincipal? user, params string[] permissions)
    {
        var cache = new FakeUserPermissionCache(permissions);
        var httpContext = user != null ? new DefaultHttpContext { User = user } : null;
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserPermissionContext(cache, accessor);
        return new CurrentUserPermissionService(context, accessor);
    }

    [Fact]
    public void PermissionEvaluator_SuperAdmin_BypassesAllChecksForKnownPermissions()
    {
        var superAdmin = CreateSuperAdmin();
        Assert.True(PermissionEvaluator.IsSuperAdmin(superAdmin));

        Assert.True(PermissionEvaluator.EvaluatePermission(null, PermissionCatalog.Property.Module, isSuperAdmin: true));
        Assert.True(PermissionEvaluator.EvaluatePermission(null, PermissionCatalog.Property.Edit, isSuperAdmin: true));
        Assert.False(PermissionEvaluator.EvaluatePermission(null, "NonExistent.Permission", isSuperAdmin: true));
        Assert.True(PermissionEvaluator.EvaluateModuleAccess(null, PermissionCatalog.Property.Module, isSuperAdmin: true));
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(null, "NonExistent.Module", isSuperAdmin: true));
        Assert.True(PermissionEvaluator.EvaluateModuleAccess(null, PermissionCatalog.Lease.Module, isSuperAdmin: true));
        Assert.True(PermissionEvaluator.EvaluatePermission(null, PermissionCatalog.Lease.Create, isSuperAdmin: true));
    }

    [Fact]
    public void PermissionEvaluator_ModuleAccess_RequiresExactOrdinalMatch()
    {
        // 1. Exact module grants module access
        var setWithProperty = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Module };
        Assert.True(PermissionEvaluator.EvaluateModuleAccess(setWithProperty, PermissionCatalog.Property.Module, false));
        Assert.True(PermissionEvaluator.EvaluatePermission(setWithProperty, PermissionCatalog.Property.Module, false));

        // 2. Action claim ALONE does NOT grant module access
        var setWithActionOnly = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Create };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(setWithActionOnly, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluatePermission(setWithActionOnly, PermissionCatalog.Property.Module, false));

        // 3. Prefix collision: Internal.PropertyType MUST NOT match Internal.Property
        var setWithPropertyType = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.PropertyType.Module };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(setWithPropertyType, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluatePermission(setWithPropertyType, PermissionCatalog.Property.Module, false));

        // 4. Prefix collision: System must not match System.User, System.User must not match System
        var setWithSystemUser = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.User.Module };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(setWithSystemUser, "System", false));
        Assert.True(PermissionEvaluator.EvaluateModuleAccess(setWithSystemUser, PermissionCatalog.User.Module, false));

        var setWithFakeSystem = new HashSet<string>(StringComparer.Ordinal) { "System" };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(setWithFakeSystem, PermissionCatalog.User.Module, false));
    }

    [Fact]
    public void PermissionEvaluator_ActionPermission_RequiresBothModuleAndAction()
    {
        // 1. Both module AND action present -> SUCCESS
        var fullyAuthorized = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit };
        Assert.True(PermissionEvaluator.EvaluatePermission(fullyAuthorized, PermissionCatalog.Property.Edit, false));

        // 2. Module only -> FAIL for action
        var moduleOnly = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Module };
        Assert.False(PermissionEvaluator.EvaluatePermission(moduleOnly, PermissionCatalog.Property.Edit, false));

        // 3. Action only -> FAIL because parent module claim is missing
        var actionOnly = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Edit };
        Assert.False(PermissionEvaluator.EvaluatePermission(actionOnly, PermissionCatalog.Property.Edit, false));

        // 4. Different action under same module -> FAIL
        var otherAction = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Module, PermissionCatalog.Property.Create };
        Assert.False(PermissionEvaluator.EvaluatePermission(otherAction, PermissionCatalog.Property.Edit, false));
    }

    [Fact]
    public void PermissionEvaluator_UnknownPermission_SafelyDenied()
    {
        var set = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit };

        Assert.False(PermissionEvaluator.EvaluatePermission(set, "Unknown.Permission.Name", false));
        Assert.False(PermissionEvaluator.EvaluatePermission(set, "Unknown.Permission.Name", isSuperAdmin: true));
        Assert.False(PermissionEvaluator.EvaluatePermission(set, "", false));
        Assert.False(PermissionEvaluator.EvaluatePermission(set, null, false));
        Assert.False(PermissionEvaluator.EvaluatePermission(null, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(null, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(set, "Unknown.Module", false));
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(set, "Unknown.Module", isSuperAdmin: true));
    }

    [Fact]
    public void PermissionEvaluator_AllRegisteredActions_HaveMappedModules()
    {
        foreach (var module in PermissionCatalog.AllModules)
        {
            foreach (var action in module.Actions)
            {
                // HasPermission on action should fail if user has only action
                var actionOnlySet = new HashSet<string>(StringComparer.Ordinal) { action };
                Assert.False(PermissionEvaluator.EvaluatePermission(actionOnlySet, action, false),
                    $"Action '{action}' granted permission without module claim '{module.Path}'!");

                // HasPermission on action should succeed if user has both module and action
                var fullSet = new HashSet<string>(StringComparer.Ordinal) { module.Path, action };
                Assert.True(PermissionEvaluator.EvaluatePermission(fullSet, action, false),
                    $"Action '{action}' failed permission even with module '{module.Path}' and action claims!");
            }
        }
    }

    private static ClaimsPrincipal CreateUnauthenticatedUser(params string[] permissions)
    {
        var claims = permissions.Select(p => new Claim(AppClaimTypes.Permission, p));
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    private static ClaimsPrincipal CreateUnauthenticatedSuperAdmin()
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim("IsSuperAdmin", "true")]));
    }

    [Fact]
    public void PermissionEvaluator_IsSuperAdmin_StrictConditions()
    {
        // 1. Null user
        Assert.False(PermissionEvaluator.IsSuperAdmin(null));

        // 2. Unauthenticated user with IsSuperAdmin claim
        var unauthSuperAdmin = CreateUnauthenticatedSuperAdmin();
        Assert.False(PermissionEvaluator.IsSuperAdmin(unauthSuperAdmin));

        // 3. Missing NameIdentifier
        var noIdUser = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "true")
        ], "TestAuth"));
        Assert.False(PermissionEvaluator.IsSuperAdmin(noIdUser));

        // 4. Missing UserType
        var noUserTypeUser = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim("IsSuperAdmin", "true")
        ], "TestAuth"));
        Assert.False(PermissionEvaluator.IsSuperAdmin(noUserTypeUser));

        // 5. UserType = Tenant (even with IsSuperAdmin = true)
        var tenantUser = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "tenant-1"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Tenant).ToString()),
            new Claim("IsSuperAdmin", "true")
        ], "TestAuth"));
        Assert.False(PermissionEvaluator.IsSuperAdmin(tenantUser));

        // 6. Invalid UserType
        var invalidUserType = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(AppClaimTypes.UserType, "not-an-int"),
            new Claim("IsSuperAdmin", "true")
        ], "TestAuth"));
        Assert.False(PermissionEvaluator.IsSuperAdmin(invalidUserType));

        // 7. IsSuperAdmin = false or wrong case
        var falseSuper = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "false")
        ], "TestAuth"));
        Assert.False(PermissionEvaluator.IsSuperAdmin(falseSuper));

        var caseSuper = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim("IsSuperAdmin", "True")
        ], "TestAuth"));
        Assert.False(PermissionEvaluator.IsSuperAdmin(caseSuper));

        // 8. Valid internal superadmin
        var validSuper = CreateSuperAdmin();
        Assert.True(PermissionEvaluator.IsSuperAdmin(validSuper));
    }

    [Fact]
    public void PermissionEvaluator_CaseSensitivityAndWildcards_AreRejected()
    {
        // Lowercase / wrong case
        var wrongCaseSet = new HashSet<string>(StringComparer.Ordinal) { "internal.property", "internal.property.edit" };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(wrongCaseSet, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluatePermission(wrongCaseSet, PermissionCatalog.Property.Edit, false));

        // Wildcard claim
        var wildcardSet = new HashSet<string>(StringComparer.Ordinal) { "Internal.Property.*" };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(wildcardSet, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluatePermission(wildcardSet, PermissionCatalog.Property.Edit, false));

        // Global wildcard
        var starSet = new HashSet<string>(StringComparer.Ordinal) { "*" };
        Assert.False(PermissionEvaluator.EvaluateModuleAccess(starSet, PermissionCatalog.Property.Module, false));
        Assert.False(PermissionEvaluator.EvaluatePermission(starSet, PermissionCatalog.Property.Edit, false));
    }

    [Fact]
    public async Task RealAuthorizationPolicy_EnforcesSameDecisionAsPermissionEvaluator()
    {
        var dynamicCache = new FakeUserPermissionCache();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ApplicationDbContext>(_ => null!);
        services.AddScoped<IUserRoleService>(_ => null!);
        services.AddSingleton<IUserPermissionCache>(dynamicCache);
        services.AddIdentityModule();

        var sp = services.BuildServiceProvider();

        // IAuthorizationService and AdminBypassHandler must be resolved from real application registrations
        var authService = sp.GetRequiredService<IAuthorizationService>();
        var handlers = sp.GetServices<IAuthorizationHandler>();
        Assert.Contains(handlers, h => h is AdminBypassHandler);

        var actionPolicy = PermissionCatalog.Property.Edit;
        var modulePolicy = PermissionCatalog.Property.Module;

        // 1. Unauthenticated superadmin -> FAILS
        var unauthSuper = CreateUnauthenticatedSuperAdmin();
        var res1 = await authService.AuthorizeAsync(unauthSuper, actionPolicy);
        Assert.False(res1.Succeeded);

        // 2. Unauthenticated user with permission -> FAILS
        var unauthUser = CreateUnauthenticatedUser(modulePolicy, actionPolicy);
        var res2 = await authService.AuthorizeAsync(unauthUser, actionPolicy);
        Assert.False(res2.Succeeded);

        // 3. Authenticated SuperAdmin -> SUCCEEDS
        var authSuper = CreateSuperAdmin();
        var res3 = await authService.AuthorizeAsync(authSuper, actionPolicy);
        Assert.True(res3.Succeeded);

        // 4. Authenticated with module + action -> SUCCEEDS
        var fullUser = CreateUserWithId("user-full", modulePolicy, actionPolicy);
        dynamicCache.SetUserPermissions("user-full", modulePolicy, actionPolicy);
        var res4 = await authService.AuthorizeAsync(fullUser, actionPolicy);
        Assert.True(res4.Succeeded);

        // 5. Authenticated with action only -> FAILS
        var actionOnly = CreateUserWithId("user-action-only", actionPolicy);
        dynamicCache.SetUserPermissions("user-action-only", actionPolicy);
        var res5 = await authService.AuthorizeAsync(actionOnly, actionPolicy);
        Assert.False(res5.Succeeded);

        // 6. Authenticated with module only -> FAILS for action, SUCCEEDS for module
        var moduleOnly = CreateUserWithId("user-module-only", modulePolicy);
        dynamicCache.SetUserPermissions("user-module-only", modulePolicy);
        var res6Action = await authService.AuthorizeAsync(moduleOnly, actionPolicy);
        var res6Module = await authService.AuthorizeAsync(moduleOnly, modulePolicy);
        Assert.False(res6Action.Succeeded);
        Assert.True(res6Module.Succeeded);

        // 7. Case sensitivity -> FAILS
        var wrongCase = CreateUserWithId("user-wrong-case", "internal.property", "internal.property.edit");
        dynamicCache.SetUserPermissions("user-wrong-case", "internal.property", "internal.property.edit");
        var res7 = await authService.AuthorizeAsync(wrongCase, actionPolicy);
        Assert.False(res7.Succeeded);

        // 8. Wildcard module claim -> FAILS
        var wildcardModule = CreateUserWithId("user-wildcard-module", "Internal.Property.*");
        dynamicCache.SetUserPermissions("user-wildcard-module", "Internal.Property.*");
        var res8Action = await authService.AuthorizeAsync(wildcardModule, actionPolicy);
        var res8Module = await authService.AuthorizeAsync(wildcardModule, modulePolicy);
        Assert.False(res8Action.Succeeded);
        Assert.False(res8Module.Succeeded);

        // 9. Global wildcard claim -> FAILS
        var globalWildcard = CreateUserWithId("user-wildcard-global", "*");
        dynamicCache.SetUserPermissions("user-wildcard-global", "*");
        var res9Action = await authService.AuthorizeAsync(globalWildcard, actionPolicy);
        var res9Module = await authService.AuthorizeAsync(globalWildcard, modulePolicy);
        Assert.False(res9Action.Succeeded);
        Assert.False(res9Module.Succeeded);
    }

    private static (TagHelperContext Context, TagHelperOutput Output) CreateTagHelperContextAndOutput(string tagName = "button", string permission = "Internal.Property.Edit")
    {
        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            tagName,
            new TagHelperAttributeList { new TagHelperAttribute("asp-permission", permission) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("Düzenle")));

        return (context, output);
    }

    [Fact]
    public async Task PermissionTagHelper_SuppressesOutputWhenUnauthorized()
    {
        // 1. Unauthenticated user -> suppressed (TagName becomes null)
        var (context1, output1) = CreateTagHelperContextAndOutput();
        var user1 = CreateUnauthenticatedUser(PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        var svc1 = CreatePermissionService(user1, PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        var helper1 = new PermissionTagHelper(svc1)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper1.ProcessAsync(context1, output1);
        Assert.Null(output1.TagName);

        // 2. Action only (missing module in cache) -> suppressed
        var (context2, output2) = CreateTagHelperContextAndOutput();
        var user2 = CreateUser(PermissionCatalog.Property.Edit);
        var svc2 = CreatePermissionService(user2, PermissionCatalog.Property.Edit);
        var helper2 = new PermissionTagHelper(svc2)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper2.ProcessAsync(context2, output2);
        Assert.Null(output2.TagName);

        // 3. Module + Action -> NOT suppressed, attribute removed
        var (context3, output3) = CreateTagHelperContextAndOutput();
        var user3 = CreateUser(PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        var svc3 = CreatePermissionService(user3, PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        var helper3 = new PermissionTagHelper(svc3)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper3.ProcessAsync(context3, output3);
        Assert.Equal("button", output3.TagName);
        Assert.DoesNotContain(output3.Attributes, a => a.Name == "asp-permission");

        // 4. Authenticated superadmin -> NOT suppressed, attribute removed
        var (context4, output4) = CreateTagHelperContextAndOutput();
        var user4 = CreateSuperAdmin();
        var svc4 = CreatePermissionService(user4);
        var helper4 = new PermissionTagHelper(svc4)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper4.ProcessAsync(context4, output4);
        Assert.Equal("button", output4.TagName);
        Assert.DoesNotContain(output4.Attributes, a => a.Name == "asp-permission");
    }

    [Fact]
    public async Task FormWritePermissionTagHelper_DisablesFormWhenUnauthorizedOrUnauthenticated()
    {
        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        // 1. Null user -> disabled fieldset and banner
        var output = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new TagHelperAttribute("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var svcNull = CreatePermissionService(null);
        var helper = new FormWritePermissionTagHelper(svcNull)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper.ProcessAsync(context, output);
        Assert.Contains("<fieldset disabled", output.PreContent.GetContent());
        Assert.Contains("Bu kaydı görüntüleyebilirsiniz ancak değiştiremezsiniz.", output.PreContent.GetContent());
        Assert.Equal("</fieldset>", output.PostContent.GetContent());

        // 2. Unauthenticated user with permission in cache -> disabled
        output = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new TagHelperAttribute("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var userUnauth = CreateUnauthenticatedUser(PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        var svcUnauth = CreatePermissionService(userUnauth, PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        helper = new FormWritePermissionTagHelper(svcUnauth)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper.ProcessAsync(context, output);
        Assert.Contains("<fieldset disabled", output.PreContent.GetContent());

        // 3. Authenticated user without write permission (module only) -> disabled
        output = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new TagHelperAttribute("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var userModOnly = CreateUser(PermissionCatalog.Property.Module);
        var svcModOnly = CreatePermissionService(userModOnly, PermissionCatalog.Property.Module);
        helper = new FormWritePermissionTagHelper(svcModOnly)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper.ProcessAsync(context, output);
        Assert.Contains("<fieldset disabled", output.PreContent.GetContent());

        // 4. Authenticated user WITH write permission (module + action) -> NOT disabled
        output = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new TagHelperAttribute("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var userFull = CreateUser(PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        var svcFull = CreatePermissionService(userFull, PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);
        helper = new FormWritePermissionTagHelper(svcFull)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper.ProcessAsync(context, output);
        Assert.Empty(output.PreContent.GetContent());
        Assert.Empty(output.PostContent.GetContent());

        // 5. Authenticated superadmin -> NOT disabled
        output = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new TagHelperAttribute("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var userAdmin = CreateSuperAdmin();
        var svcAdmin = CreatePermissionService(userAdmin);
        helper = new FormWritePermissionTagHelper(svcAdmin)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        await helper.ProcessAsync(context, output);
        Assert.Empty(output.PreContent.GetContent());
        Assert.Empty(output.PostContent.GetContent());

        // 6. Permission unspecified -> NOT disabled
        output = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new TagHelperAttribute("asp-form-write-permission", "") },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var userAny = CreateUser();
        var svcAny = CreatePermissionService(userAny);
        helper = new FormWritePermissionTagHelper(svcAny)
        {
            Permission = null
        };
        await helper.ProcessAsync(context, output);
        Assert.Empty(output.PreContent.GetContent());
        Assert.Empty(output.PostContent.GetContent());
    }
    [Fact]
    public void ReadOnlyGetEndpoints_AndMutationEndpoints_HaveExpectedPolicies()
    {
        // 1. BankTransactionController:
        // SelectMatch (GET) -> Payment.Module
        var selectMatchMethod = typeof(BankTransactionController).GetMethod(nameof(BankTransactionController.SelectMatch));
        var selectMatchPolicy = Assert.Single(selectMatchMethod!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Payment.Module, selectMatchPolicy.Policy);

        // SelectForPayment (GET) -> Payment.Module
        var selectForPaymentMethod = typeof(BankTransactionController).GetMethod(nameof(BankTransactionController.SelectForPayment));
        var selectForPaymentPolicy = Assert.Single(selectForPaymentMethod!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Payment.Module, selectForPaymentPolicy.Policy);

        // Match (POST) -> Payment.MatchBankTransaction
        var matchMethod = typeof(BankTransactionController).GetMethod(nameof(BankTransactionController.Match));
        var matchPolicy = Assert.Single(matchMethod!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Payment.MatchBankTransaction, matchPolicy.Policy);

        // Unmatch (POST) -> Payment.MatchBankTransaction
        var unmatchMethod = typeof(BankTransactionController).GetMethod(nameof(BankTransactionController.Unmatch));
        var unmatchPolicy = Assert.Single(unmatchMethod!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Payment.MatchBankTransaction, unmatchPolicy.Policy);

        // 2. ReservationController:
        // Calculate (GET) -> Reservation.Module
        var calculateMethod = typeof(ReservationController).GetMethod(nameof(ReservationController.Calculate));
        var calculatePolicy = Assert.Single(calculateMethod!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Reservation.Module, calculatePolicy.Policy);

        // Create (POST) -> Reservation.Create
        var createPostMethod = typeof(ReservationController).GetMethods()
            .Single(m => m.Name == nameof(ReservationController.Create) && m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), true).Any());
        var createPolicy = Assert.Single(createPostMethod.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Reservation.Create, createPolicy.Policy);

        // Edit (POST) -> Reservation.Edit
        var editPostMethod = typeof(ReservationController).GetMethods()
            .Single(m => m.Name == nameof(ReservationController.Edit) && m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), true).Any());
        var editPolicy = Assert.Single(editPostMethod.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PermissionCatalog.Reservation.Edit, editPolicy.Policy);
    }
}
