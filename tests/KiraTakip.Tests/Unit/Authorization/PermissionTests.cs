using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.Identity;
using KiraTakip.Services;
using KiraTakip.Services.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace KiraTakip.Tests;

public class PermissionTests
{
    // ── AdminBypassHandler ────────────────────────────────────────────────────

    private class DummyRequirement : IAuthorizationRequirement { }

    [Fact]
    public async Task AdminBypassHandler_IsSuperAdmin_TumGereksinimlerSucceedOlur()
    {
        var requirements = new List<IAuthorizationRequirement> { new DummyRequirement(), new DummyRequirement() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("IsSuperAdmin", "true")]));
        var ctx = new AuthorizationHandlerContext(requirements, principal, null);

        await new AdminBypassHandler().HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
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
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.OperasyonMuduruIzinleri));
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.All));
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
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.OperasyonMuduruIzinleri));
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.All));
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
}
