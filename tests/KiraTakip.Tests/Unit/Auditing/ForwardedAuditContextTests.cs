using System.Net;
using KiraTakip.Web.Context;
using KiraTakip.Web.DependencyInjection;
using KiraTakip.Web.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KiraTakip.Tests.Auditing;

public class ForwardedAuditContextTests
{
    [Fact]
    public void WebModule_ShouldApplyValidatedTrustedProxyConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseProxy:Enabled"] = "true",
                ["ReverseProxy:ForwardLimit"] = "2",
                ["ReverseProxy:KnownProxies:0"] = "10.0.0.10"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddWebModule(configuration);
        using var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<ReverseProxySettings>>().Value;
        var forwarded = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.True(settings.Enabled);
        Assert.Equal(2, forwarded.ForwardLimit);
        Assert.Contains(IPAddress.Parse("10.0.0.10"), forwarded.KnownProxies);
    }

    [Fact]
    public void WebModule_ShouldRejectInvalidTrustedProxyAddress()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseProxy:Enabled"] = "true",
                ["ReverseProxy:KnownProxies:0"] = "not-an-ip"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddWebModule(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<ReverseProxySettings>>().Value);
    }

    [Fact]
    public async Task TrustedProxy_ShouldReplaceRemoteIpWithForwardedClientIp()
    {
        var context = CreateHttpContext("10.0.0.10", "198.51.100.25");
        var middleware = CreateMiddleware("10.0.0.10");

        await middleware.Invoke(context);

        var requestContext = new HttpRequestContext(new HttpContextAccessor { HttpContext = context });
        Assert.Equal("198.51.100.25", requestContext.IpAddress);
    }

    [Fact]
    public async Task UntrustedClient_ShouldNotBeAbleToSpoofForwardedIp()
    {
        var context = CreateHttpContext("203.0.113.10", "198.51.100.25");
        var middleware = CreateMiddleware("10.0.0.10");

        await middleware.Invoke(context);

        var requestContext = new HttpRequestContext(new HttpContextAccessor { HttpContext = context });
        Assert.Equal("203.0.113.10", requestContext.IpAddress);
    }

    [Fact]
    public void Ipv4MappedIpv6Address_ShouldBeNormalized()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.0.2.44");

        var requestContext = new HttpRequestContext(new HttpContextAccessor { HttpContext = context });

        Assert.Equal("192.0.2.44", requestContext.IpAddress);
    }

    private static DefaultHttpContext CreateHttpContext(string remoteIp, string forwardedIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Headers["X-Forwarded-For"] = forwardedIp;
        return context;
    }

    private static ForwardedHeadersMiddleware CreateMiddleware(string trustedProxy)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
            ForwardLimit = 1
        };
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();
        options.KnownProxies.Add(IPAddress.Parse(trustedProxy));

        return new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));
    }
}
