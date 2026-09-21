using KiraTakip.Auditing;
using KiraTakip.Common;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Identity;

namespace KiraTakip.Web.Context;

public class HttpRequestContext(
    IHttpContextAccessor httpContextAccessor,
    ICurrentUserContext currentUserContext) : IRequestContext, IAuditContext
{
    public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
        : this(httpContextAccessor, new CurrentUserContext(httpContextAccessor))
    {
    }

    public string? UserId => currentUserContext.UserId;

    public UserType? UserType => currentUserContext.UserType;

    public int? TenantId => currentUserContext.TenantId;

    public string? IpAddress
    {
        get
        {
            var address = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress;
            if (address?.IsIPv4MappedToIPv6 == true)
                address = address.MapToIPv4();
            return address?.ToString();
        }
    }

    public string? UserAgent => httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();

    public string? BaseUrl
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            return request is not null
                ? $"{request.Scheme}://{request.Host}"
                : null;
        }
    }
}