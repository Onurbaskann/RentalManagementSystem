using System.Security.Claims;
using KiraTakip.Common;
using KiraTakip.Authorization;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Context;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace KiraTakip.Tests;

public class ApplicationUserManagerAdapterTests
{
    [Fact]
    public void AppIdentityResult_Success_ShouldHaveSucceededTrueAndEmptyErrors()
    {
        var result = AppIdentityResult.Success();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AppIdentityResult_Failed_ShouldHaveSucceededFalseAndSpecifiedErrors()
    {
        var result = AppIdentityResult.Failed("Hata 1", "Hata 2");

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Hata 1", result.Errors);
        Assert.Contains("Hata 2", result.Errors);
    }

    [Fact]
    public void HttpRequestContext_WithNullHttpContext_ShouldReturnNullProperties()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var context = new HttpRequestContext(accessor);

        Assert.Null(context.UserId);
        Assert.Null(context.UserType);
        Assert.Null(context.TenantId);
        Assert.Null(context.IpAddress);
        Assert.Null(context.UserAgent);
        Assert.Null(context.BaseUrl);
    }

    [Fact]
    public void HttpRequestContext_WithPopulatedHttpContext_ShouldReturnExpectedProperties()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(AppClaimTypes.UserType, ((int)UserType.Tenant).ToString()),
            new Claim(AppClaimTypes.TenantId, "42")
        ], "Test"));
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new HttpRequestContext(accessor);

        Assert.Equal("user-123", context.UserId);
        Assert.Equal(UserType.Tenant, context.UserType);
        Assert.Equal(42, context.TenantId);
        Assert.Equal("127.0.0.1", context.IpAddress);
        Assert.Equal("TestAgent/1.0", context.UserAgent);
        Assert.Equal("https://example.com", context.BaseUrl);
    }

    [Theory]
    [InlineData("user@domain.com", "USER@DOMAIN.COM")]
    [InlineData("USER@DOMAIN.COM", "USER@DOMAIN.COM")]
    [InlineData("u.s.e.r+tag@domain.co.uk", "U.S.E.R+TAG@DOMAIN.CO.UK")]
    [InlineData("TEST.User_123@Sub.Domain.Org", "TEST.USER_123@SUB.DOMAIN.ORG")]
    public void NormalizeEmail_WithVariousEmails_ShouldReturnNonNullNormalizedEmail(string input, string expected)
    {
        var userManager = new UserManager<ApplicationUser>(
            new TestUserStore(),
            null!,
            null!,
            null!,
            null!,
            new UpperInvariantLookupNormalizer(),
            null!,
            null!,
            null!);
        var adapter = new KiraTakip.Infrastructure.Identity.ApplicationUserManagerAdapter(userManager);

        var result = adapter.NormalizeEmail(input);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    private sealed class TestUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
    }
}
