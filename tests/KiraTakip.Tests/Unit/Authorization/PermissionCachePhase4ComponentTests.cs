using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using KiraTakip.Authorization;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.DependencyInjection;
using KiraTakip.Web.TagHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace KiraTakip.Tests;

public class PermissionCachePhase4ComponentTests
{
    private readonly ITestOutputHelper _output;

    public PermissionCachePhase4ComponentTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // â”€â”€ Test Cache Helper â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private sealed class DynamicTestPermissionCache : IUserPermissionCache
    {
        public Dictionary<string, HashSet<string>> Store { get; } = new(StringComparer.Ordinal);

        public void Set(string userId, params string[] permissions)
        {
            Store[userId] = new HashSet<string>(permissions, StringComparer.Ordinal);
        }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (Store.TryGetValue(userId, out var perms))
            {
                return Task.FromResult<IReadOnlySet<string>>(perms);
            }
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        }

        public void Invalidate(string userId) => Store.Remove(userId);
        public void InvalidateMany(IEnumerable<string> userIds)
        {
            foreach (var id in userIds) Store.Remove(id);
        }
    }

    private static (IAuthorizationService AuthService, DynamicTestPermissionCache Cache, DefaultHttpContext HttpContext, ICurrentUserPermissionService PermService)
        CreateComponentContext(ClaimsPrincipal? principal)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<Data.ApplicationDbContext>(_ => null!);
        services.AddScoped<IUserRoleService>(_ => null!);

        var cache = new DynamicTestPermissionCache();
        services.AddSingleton<IUserPermissionCache>(cache);

        var httpContext = new DefaultHttpContext();
        if (principal != null)
        {
            httpContext.User = principal;
        }
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

        services.AddIdentityModule();

        var sp = services.BuildServiceProvider();
        var authService = sp.GetRequiredService<IAuthorizationService>();
        var permService = sp.GetRequiredService<ICurrentUserPermissionService>();
        return (authService, cache, httpContext, permService);
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // 3. ENDPOINT / MENÃœ / BUTON TUTARLILIÄI
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [Fact]
    public void ControllerActions_HaveExpectedAuthorizePolicies_ViaReflection()
    {
        // 1. Internal Module: PropertyController
        var propType = typeof(PropertyController);
        var propClassAuth = propType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(propClassAuth);

        var propIndexMethod = propType.GetMethod(nameof(PropertyController.Index));
        Assert.NotNull(propIndexMethod);
        var propIndexAuth = propIndexMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(propIndexAuth);
        Assert.Equal(PermissionCatalog.Property.Module, propIndexAuth.Policy);

        var propEditMethod = propType.GetMethod(nameof(PropertyController.Edit), [typeof(Web.Models.ViewModels.EditPropertyViewModel)]);
        Assert.NotNull(propEditMethod);
        var propEditAuth = propEditMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(propEditAuth);
        Assert.Equal(PermissionCatalog.Property.Edit, propEditAuth.Policy);

        // 2. Tenant Module: TenantRoleController
        var tenantRoleType = typeof(TenantRoleController);
        var tenantRoleClassAuth = tenantRoleType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(tenantRoleClassAuth);
        Assert.Equal("TenantUser", tenantRoleClassAuth.Policy);

        var tenantIndexMethod = tenantRoleType.GetMethod(nameof(TenantRoleController.Index));
        Assert.NotNull(tenantIndexMethod);
        var tenantIndexAuth = tenantIndexMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(tenantIndexAuth);
        Assert.Equal(PermissionCatalog.TenantPortal.System.Role.Module, tenantIndexAuth.Policy);

        var tenantCreateMethod = tenantRoleType.GetMethod(nameof(TenantRoleController.Create), [typeof(Web.Models.ViewModels.TenantRoleFormViewModel)]);
        Assert.NotNull(tenantCreateMethod);
        var tenantCreateAuth = tenantCreateMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(tenantCreateAuth);
        Assert.Equal(PermissionCatalog.TenantPortal.System.Role.Create, tenantCreateAuth.Policy);
    }

    [Fact]
    public async Task EndpointAndUI_ModuleOnly_PermitsView_DeniesWriteAndSuppressesButton()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-module-only"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        ], "Cookies"));

        var (authService, cache, _, permService) = CreateComponentContext(user);
        cache.Set("user-module-only", PermissionCatalog.Property.Module);

        // 1. Endpoint policy checks
        var viewAuth = await authService.AuthorizeAsync(user, PermissionCatalog.Property.Module);
        var writeAuth = await authService.AuthorizeAsync(user, PermissionCatalog.Property.Edit);
        Assert.True(viewAuth.Succeeded);
        Assert.False(writeAuth.Succeeded);

        // 2. UI PermissionTagHelper check: button must be suppressed
        var tagHelper = new PermissionTagHelper(permService)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
        var output = new TagHelperOutput(
            "button",
            new TagHelperAttributeList { new("asp-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("DÃ¼zenle")));

        await tagHelper.ProcessAsync(context, output);
        Assert.Null(output.TagName); // Output suppressed

        // 3. UI FormWritePermissionTagHelper check: form wrapped in disabled fieldset with warning banner
        var formTagHelper = new FormWritePermissionTagHelper(permService)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        var formOutput = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("<input type='text'/>")));

        await formTagHelper.ProcessAsync(context, formOutput);
        var preHtml = formOutput.PreContent.GetContent();
        var postHtml = formOutput.PostContent.GetContent();
        Assert.Contains("<fieldset disabled", preHtml);
        Assert.Contains("bg-amber-50", preHtml);
        Assert.Contains("</fieldset>", postHtml);
    }

    [Fact]
    public async Task EndpointAndUI_ModuleAndAction_PermitsViewAndWrite_RendersButton()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-full"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        ], "Cookies"));

        var (authService, cache, _, permService) = CreateComponentContext(user);
        cache.Set("user-full", PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit);

        // 1. Endpoint policy checks
        var viewAuth = await authService.AuthorizeAsync(user, PermissionCatalog.Property.Module);
        var writeAuth = await authService.AuthorizeAsync(user, PermissionCatalog.Property.Edit);
        Assert.True(viewAuth.Succeeded);
        Assert.True(writeAuth.Succeeded);

        // 2. UI PermissionTagHelper check: button must NOT be suppressed
        var tagHelper = new PermissionTagHelper(permService)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
        var output = new TagHelperOutput(
            "button",
            new TagHelperAttributeList { new("asp-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("DÃ¼zenle")));

        await tagHelper.ProcessAsync(context, output);
        Assert.Equal("button", output.TagName);
        Assert.False(output.Attributes.ContainsName("asp-permission"));

        // 3. UI FormWritePermissionTagHelper check: form not disabled
        var formTagHelper = new FormWritePermissionTagHelper(permService)
        {
            Permission = PermissionCatalog.Property.Edit
        };
        var formOutput = new TagHelperOutput(
            "form",
            new TagHelperAttributeList { new("asp-form-write-permission", PermissionCatalog.Property.Edit) },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("<input type='text'/>")));

        await formTagHelper.ProcessAsync(context, formOutput);
        Assert.Empty(formOutput.PreContent.GetContent());
        Assert.Empty(formOutput.PostContent.GetContent());
    }

    [Fact]
    public async Task EndpointAndUI_ActionOnlyWithoutModule_DeniesAccess()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-action-only"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        ], "Cookies"));

        var (authService, cache, _, permService) = CreateComponentContext(user);
        cache.Set("user-action-only", PermissionCatalog.Property.Edit); // Action present, Module absent!

        var writeAuth = await authService.AuthorizeAsync(user, PermissionCatalog.Property.Edit);
        Assert.False(writeAuth.Succeeded);

        var hasPerm = await permService.HasPermissionAsync(user, PermissionCatalog.Property.Edit);
        Assert.False(hasPerm);
    }

    [Fact]
    public async Task TenantUser_WithIsSuperAdminTrue_DoesNotGainInternalBypass()
    {
        var tenantAdmin = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "tenant-fake-super"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Tenant).ToString()),
            new Claim("IsSuperAdmin", "true")
        ], "Cookies"));

        var (authService, _, _, permService) = CreateComponentContext(tenantAdmin);

        // Attempting internal property access
        var authResult = await authService.AuthorizeAsync(tenantAdmin, PermissionCatalog.Property.Edit);
        Assert.False(authResult.Succeeded);

        var permResult = await permService.HasPermissionAsync(tenantAdmin, PermissionCatalog.Property.Edit);
        Assert.False(permResult);
    }

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    // 4. COOKIE BOYUTU Ã–LÃ‡ÃœMÃœ VE KARÅžILAÅžTIRMASI
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    private sealed class FakeMeasurementUserStore :
        IUserStore<ApplicationUser>,
        IUserSecurityStampStore<ApplicationUser>,
        IUserClaimStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken) { user.SecurityStamp = stamp; return Task.CompletedTask; }
        public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.SecurityStamp ?? "stamp-default");
        public Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<IList<Claim>>([]);
        public Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken) => Task.FromResult<IList<ApplicationUser>>([]);
    }

    private sealed class FakeMeasurementUserRoleService : IUserRoleService
    {
        public Task<IList<string>> GetUserRolesAsync(string userId)
        {
            IList<string> roles = userId.Contains("tenant", StringComparison.OrdinalIgnoreCase)
                ? ["Kiraci"]
                : ["Yonetici"];
            return Task.FromResult(roles);
        }
        public Task<bool> IsInRoleAsync(string userId, string roleName) => Task.FromResult(true);
        public Task AddRoleByNameAsync(string userId, string roleName, string? atayanUserId = null) => Task.CompletedTask;
        public Task AddRoleByRolIdAsync(string userId, int rolId, string? atayanUserId = null, RoleAssignmentOperation operation = RoleAssignmentOperation.UserEdit) => Task.CompletedTask;
        public Task RemoveRoleByNameAsync(string userId, string roleName) => Task.CompletedTask;
        public Task RemoveAllRolesAsync(string userId) => Task.CompletedTask;
        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName) => Task.FromResult<IList<ApplicationUser>>([]);
        public Task<IList<string>> GetUserPermissionsFromRolesAsync(string userId) => Task.FromResult<IList<string>>([]);
    }

    public record CookieScenarioResult(
        string ScenarioName,
        int PermissionCount,
        int OldProtectedTicketLength,
        int NewProtectedTicketLength,
        int ReductionChars,
        double ReductionPercent,
        int OldTotalHeaderLength,
        int NewTotalHeaderLength,
        int OldTotalHeaderCount,
        int NewTotalHeaderCount,
        int OldDataChunkCount,
        int NewDataChunkCount,
        int OldChunkIndicatorHeaderCount,
        int NewChunkIndicatorHeaderCount,
        string? OldChunkIndicatorHeader);

    public static async Task<CookieScenarioResult> MeasureScenarioAsync(
        CookieAuthenticationOptions cookieOptions,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        string scenarioName,
        ApplicationUser user,
        string[] permissions)
    {
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero),
            ExpiresUtc = new DateTimeOffset(2026, 9, 17, 18, 0, 0, TimeSpan.Zero),
            AllowRefresh = true
        };

        // 1. NEW Structure: Generate principal via real claims factory (PermissionClaimsTransformer)
        var newPrincipal = await claimsFactory.CreateAsync(user);
        var newTicket = new AuthenticationTicket(newPrincipal, authProperties, IdentityConstants.ApplicationScheme);
        var newProtected = cookieOptions.TicketDataFormat.Protect(newTicket);

        var cookieName = cookieOptions.Cookie.Name ?? ".AspNetCore.Identity.Application";
        var httpCtxNew = new DefaultHttpContext();
        var cookieOptionsBuilt = cookieOptions.Cookie.Build(httpCtxNew);
        cookieOptions.CookieManager.AppendResponseCookie(httpCtxNew, cookieName, newProtected, cookieOptionsBuilt);
        var newHeaders = httpCtxNew.Response.Headers["Set-Cookie"].ToArray();
        var newTotalHeaderLength = newHeaders.Sum(s => s?.Length ?? 0);
        var newIndicatorHeader = newHeaders.FirstOrDefault(h =>
            h != null && (h.StartsWith($"{cookieName}=chunks-", StringComparison.OrdinalIgnoreCase) ||
                         h.StartsWith($"{cookieName}=chunks:", StringComparison.OrdinalIgnoreCase)));
        var newIndicatorCount = newIndicatorHeader != null ? 1 : 0;
        var newDataChunkCount = newHeaders.Length - newIndicatorCount;

        // 2. OLD Structure: Simulated by cloning the real principal and appending legacy permission claims
        var oldPrincipal = newPrincipal.Clone();
        var oldIdentity = (ClaimsIdentity)oldPrincipal.Identity!;
        if (!user.IsSuperAdmin)
        {
            foreach (var p in permissions)
            {
                oldIdentity.AddClaim(new Claim(AppClaimTypes.Permission, p));
            }
        }
        var oldTicket = new AuthenticationTicket(oldPrincipal, authProperties, IdentityConstants.ApplicationScheme);
        var oldProtected = cookieOptions.TicketDataFormat.Protect(oldTicket);

        var httpCtxOld = new DefaultHttpContext();
        cookieOptions.CookieManager.AppendResponseCookie(httpCtxOld, cookieName, oldProtected, cookieOptionsBuilt);
        var oldHeaders = httpCtxOld.Response.Headers["Set-Cookie"].ToArray();
        var oldTotalHeaderLength = oldHeaders.Sum(s => s?.Length ?? 0);
        var oldIndicatorHeader = oldHeaders.FirstOrDefault(h =>
            h != null && (h.StartsWith($"{cookieName}=chunks-", StringComparison.OrdinalIgnoreCase) ||
                         h.StartsWith($"{cookieName}=chunks:", StringComparison.OrdinalIgnoreCase)));
        var oldIndicatorCount = oldIndicatorHeader != null ? 1 : 0;
        var oldDataChunkCount = oldHeaders.Length - oldIndicatorCount;

        var reductionChars = oldProtected.Length - newProtected.Length;
        var reductionPct = oldProtected.Length > 0 ? (double)reductionChars / oldProtected.Length * 100.0 : 0.0;

        return new CookieScenarioResult(
            scenarioName,
            permissions.Length,
            oldProtected.Length,
            newProtected.Length,
            reductionChars,
            reductionPct,
            oldTotalHeaderLength,
            newTotalHeaderLength,
            oldHeaders.Length,
            newHeaders.Length,
            oldDataChunkCount,
            newDataChunkCount,
            oldIndicatorCount,
            newIndicatorCount,
            oldIndicatorHeader);
    }

    [Fact]
    public async Task CookieSize_Measurement_AcrossFourScenarios()
    {
        var tempKeyDir = Path.Combine(Path.GetTempPath(), "kt_phase4_cookie_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempKeyDir);

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            // Isolated test-specific Data Protection key directory (does not affect production keys)
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(tempKeyDir))
                .SetApplicationName("KiraTakip");

            services.AddScoped<Data.ApplicationDbContext>(_ => null!);
            services.AddSingleton<IUserPermissionCache>(new DynamicTestPermissionCache());
            services.AddIdentityModule();

            // Override user store and role service with test implementations
            services.AddScoped<IUserStore<ApplicationUser>, FakeMeasurementUserStore>();
            services.AddScoped<IUserRoleService, FakeMeasurementUserRoleService>();

            var sp = services.BuildServiceProvider();

            // Resolve real configured CookieAuthenticationOptions for Identity application scheme
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var cookieOptions = optionsMonitor.Get(IdentityConstants.ApplicationScheme);
            Assert.NotNull(cookieOptions.TicketDataFormat);
            Assert.NotNull(cookieOptions.CookieManager);

            // Resolve real claims principal factory (PermissionClaimsTransformer)
            var claimsFactory = sp.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();

            // Scenario 1: Normal Internal User (Few permissions: 3)
            var user1 = new ApplicationUser
            {
                Id = "usr-int-few",
                UserName = "few@kiratakip.com",
                AdSoyad = "İç Kullanıcı (Az)",
                UserType = UserType.Internal,
                SecurityStamp = "stamp-few-123",
                IsSuperAdmin = false
            };
            var res1 = await MeasureScenarioAsync(
                cookieOptions, claimsFactory,
                "Normal İç Kullanıcı (3 İzin)",
                user1,
                [PermissionCatalog.Property.Module, PermissionCatalog.Property.Create, PermissionCatalog.Property.Edit]);

            // Scenario 2: Comprehensive Internal User (PermissionCatalog.InternalAll: dynamic count)
            var user2 = new ApplicationUser
            {
                Id = "usr-int-all",
                UserName = "internalall@kiratakip.com",
                AdSoyad = "Kapsamlı İç Kullanıcı",
                UserType = UserType.Internal,
                SecurityStamp = "stamp-intall-456",
                IsSuperAdmin = false
            };
            var res2 = await MeasureScenarioAsync(
                cookieOptions, claimsFactory,
                $"Kapsamlı İç Kullanıcı (InternalAll - {PermissionCatalog.InternalAll.Count} İzin)",
                user2,
                PermissionCatalog.InternalAll.ToArray());

            // Scenario 3: Normal Tenant User (PermissionCatalog.TenantAll: dynamic count)
            var user3 = new ApplicationUser
            {
                Id = "usr-tenant-all",
                UserName = "tenantall@kiratakip.com",
                AdSoyad = "Kiracı Kullanıcısı",
                UserType = UserType.Tenant,
                TenantId = 1001,
                SecurityStamp = "stamp-tenant-789",
                IsSuperAdmin = false
            };
            var res3 = await MeasureScenarioAsync(
                cookieOptions, claimsFactory,
                $"Kiracı Kullanıcısı (TenantAll - {PermissionCatalog.TenantAll.Count} İzin)",
                user3,
                PermissionCatalog.TenantAll.ToArray());

            // Scenario 4: Internal Super Admin (Control - 0 permission claims)
            var user4 = new ApplicationUser
            {
                Id = "usr-superadmin",
                UserName = "superadmin@kiratakip.com",
                AdSoyad = "İç Süper Admin",
                UserType = UserType.Internal,
                SecurityStamp = "stamp-superadmin-000",
                IsSuperAdmin = true
            };
            var res4 = await MeasureScenarioAsync(
                cookieOptions, claimsFactory,
                "İç Süper Admin (Kontrol - 0 İzin Claim'i)",
                user4,
                []);

            _output.WriteLine($"[Senaryo 1: Normal İç] İzin: {res1.PermissionCount}, Eski: {res1.OldProtectedTicketLength} kar., Yeni: {res1.NewProtectedTicketLength} kar., Azalma: {res1.ReductionChars} kar. (%{res1.ReductionPercent:F1}), Başlık: {res1.OldTotalHeaderCount} -> {res1.NewTotalHeaderCount}, Chunk: {res1.OldDataChunkCount} -> {res1.NewDataChunkCount}, Gösterge: {res1.OldChunkIndicatorHeaderCount} -> {res1.NewChunkIndicatorHeaderCount}");
            _output.WriteLine($"[Senaryo 2: Kapsamlı İç] İzin: {res2.PermissionCount}, Eski: {res2.OldProtectedTicketLength} kar., Yeni: {res2.NewProtectedTicketLength} kar., Azalma: {res2.ReductionChars} kar. (%{res2.ReductionPercent:F1}), Başlık: {res2.OldTotalHeaderCount} -> {res2.NewTotalHeaderCount}, Chunk: {res2.OldDataChunkCount} -> {res2.NewDataChunkCount}, Gösterge: {res2.OldChunkIndicatorHeaderCount} -> {res2.NewChunkIndicatorHeaderCount}");
            _output.WriteLine($"[Senaryo 3: Kiracı] İzin: {res3.PermissionCount}, Eski: {res3.OldProtectedTicketLength} kar., Yeni: {res3.NewProtectedTicketLength} kar., Azalma: {res3.ReductionChars} kar. (%{res3.ReductionPercent:F1}), Başlık: {res3.OldTotalHeaderCount} -> {res3.NewTotalHeaderCount}, Chunk: {res3.OldDataChunkCount} -> {res3.NewDataChunkCount}, Gösterge: {res3.OldChunkIndicatorHeaderCount} -> {res3.NewChunkIndicatorHeaderCount}");
            _output.WriteLine($"[Senaryo 4: Süper Admin] İzin: {res4.PermissionCount}, Eski: {res4.OldProtectedTicketLength} kar., Yeni: {res4.NewProtectedTicketLength} kar., Azalma: {res4.ReductionChars} kar. (%{res4.ReductionPercent:F1}), Başlık: {res4.OldTotalHeaderCount} -> {res4.NewTotalHeaderCount}, Chunk: {res4.OldDataChunkCount} -> {res4.NewDataChunkCount}, Gösterge: {res4.OldChunkIndicatorHeaderCount} -> {res4.NewChunkIndicatorHeaderCount}");

            // Assertions
            Assert.True(res1.OldProtectedTicketLength > res1.NewProtectedTicketLength);
            Assert.True(res2.OldProtectedTicketLength > res2.NewProtectedTicketLength);
            Assert.True(res3.OldProtectedTicketLength > res3.NewProtectedTicketLength);
            // SuperAdmin carries no permission claims in either old or new, so difference is 0
            Assert.Equal(0, res4.ReductionChars);
            Assert.Equal(res4.OldProtectedTicketLength, res4.NewProtectedTicketLength);

            // Large internal user reduction must be substantial (> 60%)
            Assert.True(res2.ReductionPercent > 60.0);

            // Detailed chunk assertions
            // Scenario 2: Comprehensive internal user
            Assert.Equal(3, res2.OldTotalHeaderCount);
            Assert.Equal(2, res2.OldDataChunkCount);
            Assert.Equal(1, res2.OldChunkIndicatorHeaderCount);
            Assert.Equal(1, res2.NewTotalHeaderCount);
            Assert.Equal(1, res2.NewDataChunkCount);
            Assert.Equal(0, res2.NewChunkIndicatorHeaderCount);
        }
        finally
        {
            if (Directory.Exists(tempKeyDir))
            {
                Directory.Delete(tempKeyDir, true);
            }
        }
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // 5. ESKÄ° COOKIE GEÃ‡Ä°ÅÄ° VE CALLBACK DOÄRULAMASI
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [Fact]
    public async Task LegacyCookieMigration_ClaimsStripped_ShouldRenewSet_CoreIdentityPreserved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<Data.ApplicationDbContext>(_ => null!);
        services.AddScoped<IUserRoleService>(_ => null!);
        services.AddSingleton<IUserPermissionCache>(new DynamicTestPermissionCache());

        var testValidator = new TestSecurityStampValidator();
        services.AddIdentityModule();
        services.AddScoped<ISecurityStampValidator>(_ => testValidator);

        var sp = services.BuildServiceProvider();
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var cookieOptions = optionsMonitor.Get(IdentityConstants.ApplicationScheme);
        var callback = cookieOptions.Events.OnValidatePrincipal;
        Assert.NotNull(callback);

        // Principal with legacy claims
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "migrated-user-1"),
            new Claim(ClaimTypes.Name, "migrated@example.com"),
            new Claim(AppClaimTypes.DisplayName, "GeÃ§iÅŸ KullanÄ±cÄ±sÄ±"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Module),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Edit)
        ], IdentityConstants.ApplicationScheme);

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, cookieOptions, ticket);

        await callback(context);

        // 1. Validator called
        Assert.Equal(1, testValidator.Calls);
        // 2. Cookie renewed
        Assert.True(context.ShouldRenew);
        // 3. Permission claims stripped
        Assert.Empty(context.Principal!.FindAll(AppClaimTypes.Permission));
        // 4. Core identity preserved
        Assert.Equal("migrated-user-1", context.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("migrated@example.com", context.Principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("GeÃ§iÅŸ KullanÄ±cÄ±sÄ±", context.Principal.FindFirstValue(AppClaimTypes.DisplayName));
        Assert.Equal(((int)UserType.Internal).ToString(), context.Principal.FindFirstValue(AppClaimTypes.UserType));
        Assert.Contains(context.Principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    private sealed class TestSecurityStampValidator : ISecurityStampValidator
    {
        public int Calls { get; private set; }
        public Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}