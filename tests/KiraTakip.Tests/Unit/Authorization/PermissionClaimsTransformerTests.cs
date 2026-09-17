using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KiraTakip.Tests;

public class PermissionClaimsTransformerTests
{
    private sealed class FakeUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
    }

    private sealed class TrackingUserRoleService : IUserRoleService
    {
        private readonly IList<string> _roles;
        public bool GetUserPermissionsFromRolesCalled { get; private set; }

        public TrackingUserRoleService(params string[] roles)
        {
            _roles = roles.ToList();
        }

        public Task<IList<string>> GetUserRolesAsync(string userId) => Task.FromResult(_roles);
        public Task<bool> IsInRoleAsync(string userId, string roleName) => Task.FromResult(_roles.Contains(roleName));
        public Task AddRoleByNameAsync(string userId, string roleName, string? atayanUserId = null) => Task.CompletedTask;
        public Task RemoveRoleByNameAsync(string userId, string roleName) => Task.CompletedTask;
        public Task RemoveAllRolesAsync(string userId) => Task.CompletedTask;
        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName) => Task.FromResult<IList<ApplicationUser>>(new List<ApplicationUser>());
        public Task<IList<string>> GetUserPermissionsFromRolesAsync(string userId)
        {
            GetUserPermissionsFromRolesCalled = true;
            return Task.FromResult<IList<string>>(new List<string> { PermissionCatalog.Property.Edit, PermissionCatalog.Property.Module });
        }
        public Task AddRoleByRolIdAsync(string userId, int rolId, string? atayanUserId = null, RoleAssignmentOperation operation = RoleAssignmentOperation.UserEdit) => Task.CompletedTask;
    }

    private static UserManager<ApplicationUser> CreateUserManager()
    {
        var store = new FakeUserStore();
        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new NullLogger<UserManager<ApplicationUser>>());
    }

    private sealed class TestSecurityStampValidator : ISecurityStampValidator
    {
        public int ValidatePrincipalCalls { get; private set; }
        public Action<CookieValidatePrincipalContext>? OnValidate { get; set; }

        public Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            ValidatePrincipalCalls++;
            OnValidate?.Invoke(context);
            return Task.CompletedTask;
        }
    }

    private sealed class DummyUserPermissionCache : IUserPermissionCache
    {
        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        public void Invalidate(string userId) { }
        public void InvalidateMany(IEnumerable<string> userIds) { }
    }

    private static (Func<CookieValidatePrincipalContext, Task> Callback, CookieAuthenticationOptions Options, IServiceProvider Sp)
        ResolveRegisteredCookieValidation(TestSecurityStampValidator validator)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ApplicationDbContext>(_ => null!);
        services.AddScoped<IUserRoleService>(_ => null!);
        services.AddSingleton<IUserPermissionCache>(new DummyUserPermissionCache());
        services.AddIdentityModule();
        services.AddScoped<ISecurityStampValidator>(_ => validator);

        var sp = services.BuildServiceProvider();
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var cookieOptions = optionsMonitor.Get(IdentityConstants.ApplicationScheme);
        var callback = cookieOptions.Events.OnValidatePrincipal;
        Assert.NotNull(callback);

        return (callback, cookieOptions, sp);
    }

    // ── Cookie Validation / Sanitization Tests ───────────────────────────────

    [Fact]
    public async Task OnValidatePrincipal_WithLegacyPermissionClaims_StripsClaimsAndSetsShouldRenew()
    {
        var validator = new TestSecurityStampValidator();
        var (callback, options, sp) = ResolveRegisteredCookieValidation(validator);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "LegacyUser"),
            new Claim(AppClaimTypes.DisplayName, "Test Legacy User"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Module),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Edit)
        }, IdentityConstants.ApplicationScheme);

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, options, ticket);

        await callback(context);

        Assert.Equal(1, validator.ValidatePrincipalCalls);
        Assert.True(context.ShouldRenew);

        // Permission claims stripped
        Assert.Empty(context.Principal!.FindAll(AppClaimTypes.Permission));

        // Core identity preserved
        Assert.Equal("user-123", context.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("LegacyUser", context.Principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("Test Legacy User", context.Principal.FindFirstValue(AppClaimTypes.DisplayName));
        Assert.Equal(((int)UserType.Internal).ToString(), context.Principal.FindFirstValue(AppClaimTypes.UserType));
        Assert.Contains(context.Principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task OnValidatePrincipal_MultipleIdentities_StripsPermissionClaimsFromAll()
    {
        var validator = new TestSecurityStampValidator();
        var (callback, options, sp) = ResolveRegisteredCookieValidation(validator);

        var id1 = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-multi"),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Module)
        }, "Identity1");

        var id2 = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Editor"),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Edit)
        }, "Identity2");

        var principal = new ClaimsPrincipal(new[] { id1, id2 });
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, options, ticket);

        await callback(context);

        Assert.True(context.ShouldRenew);
        Assert.Empty(context.Principal!.FindAll(AppClaimTypes.Permission));
        Assert.Empty(id1.FindAll(AppClaimTypes.Permission));
        Assert.Empty(id2.FindAll(AppClaimTypes.Permission));
        Assert.Equal("user-multi", id1.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Editor", id2.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public async Task OnValidatePrincipal_WithoutLegacyPermissionClaims_DoesNotSetShouldRenew()
    {
        var validator = new TestSecurityStampValidator();
        var (callback, options, sp) = ResolveRegisteredCookieValidation(validator);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-clean"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Internal).ToString())
        }, IdentityConstants.ApplicationScheme);

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, options, ticket);

        await callback(context);

        Assert.Equal(1, validator.ValidatePrincipalCalls);
        Assert.False(context.ShouldRenew);
    }

    [Fact]
    public async Task OnValidatePrincipal_WhenValidatorRejectsPrincipal_DoesNotRevalidateOrRecreatePrincipal()
    {
        var validator = new TestSecurityStampValidator
        {
            OnValidate = ctx => ctx.RejectPrincipal()
        };
        var (callback, options, sp) = ResolveRegisteredCookieValidation(validator);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-reject"),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Module)
        }, IdentityConstants.ApplicationScheme);

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, options, ticket);

        await callback(context);

        Assert.Equal(1, validator.ValidatePrincipalCalls);
        Assert.Null(context.Principal);
        Assert.False(context.ShouldRenew);
    }

    [Fact]
    public async Task OnValidatePrincipal_WhenValidatorReplacesPrincipal_CleansUpdatedPrincipal()
    {
        var replacedIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-replaced"),
            new Claim(AppClaimTypes.Permission, PermissionCatalog.Property.Edit)
        }, IdentityConstants.ApplicationScheme);

        var validator = new TestSecurityStampValidator
        {
            OnValidate = ctx => ctx.ReplacePrincipal(new ClaimsPrincipal(replacedIdentity))
        };
        var (callback, options, sp) = ResolveRegisteredCookieValidation(validator);

        var initialIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-initial")
        }, IdentityConstants.ApplicationScheme);

        var principal = new ClaimsPrincipal(initialIdentity);
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, options, ticket);

        await callback(context);

        Assert.Equal(1, validator.ValidatePrincipalCalls);
        Assert.NotNull(context.Principal);
        Assert.Equal("user-replaced", context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Empty(context.Principal.FindAll(AppClaimTypes.Permission));
        Assert.True(context.ShouldRenew);
    }

    // ── PermissionClaimsTransformer Tests ────────────────────────────────────

    [Fact]
    public async Task PermissionClaimsTransformer_NormalUser_DoesNotCallGetUserPermissionsFromRoles_AndEmitsNoPermissionClaims()
    {
        var userManager = CreateUserManager();
        var userRoleService = new TrackingUserRoleService("Editor", "Finance");
        var options = Options.Create(new IdentityOptions());

        var transformer = new PermissionClaimsTransformer(userManager, options, userRoleService);

        var user = new ApplicationUser
        {
            Id = "normal-user-1",
            UserName = "normaluser",
            AdSoyad = "Normal User",
            UserType = UserType.Internal,
            IsSuperAdmin = false
        };

        var principal = await transformer.CreateAsync(user);

        // Core assertions
        Assert.False(userRoleService.GetUserPermissionsFromRolesCalled);
        Assert.Empty(principal.FindAll(AppClaimTypes.Permission));

        // Preserved metadata
        Assert.Equal("normal-user-1", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Normal User", principal.FindFirstValue(AppClaimTypes.DisplayName));
        Assert.Equal(((int)UserType.Internal).ToString(), principal.FindFirstValue(AppClaimTypes.UserType));
        Assert.Null(principal.FindFirst("IsSuperAdmin"));
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Editor");
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Finance");
    }

    [Fact]
    public async Task PermissionClaimsTransformer_SuperAdminUser_PreservesMetadataAndEmitsNoPermissionClaims()
    {
        var userManager = CreateUserManager();
        var userRoleService = new TrackingUserRoleService("Yonetici");
        var options = Options.Create(new IdentityOptions());

        var transformer = new PermissionClaimsTransformer(userManager, options, userRoleService);

        var user = new ApplicationUser
        {
            Id = "super-user-1",
            UserName = "superadmin",
            AdSoyad = "Super Admin User",
            UserType = UserType.Internal,
            IsSuperAdmin = true
        };

        var principal = await transformer.CreateAsync(user);

        Assert.False(userRoleService.GetUserPermissionsFromRolesCalled);
        Assert.Empty(principal.FindAll(AppClaimTypes.Permission));
        Assert.Equal("true", principal.FindFirstValue("IsSuperAdmin"));
        Assert.Equal(((int)UserType.Internal).ToString(), principal.FindFirstValue(AppClaimTypes.UserType));
        Assert.Equal("Super Admin User", principal.FindFirstValue(AppClaimTypes.DisplayName));
    }
}