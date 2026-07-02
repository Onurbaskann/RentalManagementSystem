using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using System.Security.Claims;

namespace KiraTakip.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User is { } u && u.Identity?.IsAuthenticated == true ? u : null;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public UserType? UserType
    {
        get
        {
            var s = Principal?.FindFirstValue(AppClaimTypes.UserType);
            return int.TryParse(s, out var v) ? (Models.UserType)v : null;
        }
    }

    public int? KiraciId
    {
        get
        {
            var s = Principal?.FindFirstValue(AppClaimTypes.KiraciId);
            return int.TryParse(s, out var v) ? v : null;
        }
    }

    public bool IsKiraciUser => UserType == Models.UserType.Tenant && KiraciId.HasValue;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
}
