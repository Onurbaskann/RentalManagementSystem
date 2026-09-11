using KiraTakip.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace KiraTakip.Web.Context;

public class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

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