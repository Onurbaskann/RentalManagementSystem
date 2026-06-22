using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace KiraTakip.Authorization;

public class YetkiKapsamiActionFilter : IAsyncActionFilter
{
    private readonly IYetkiKapsamiCache _cache;
    private readonly IYetkiKapsamiProvider _provider;

    public YetkiKapsamiActionFilter(IYetkiKapsamiCache cache, IYetkiKapsamiProvider provider)
    {
        _cache = cache;
        _provider = provider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true
            && !user.HasClaim(c => c.Type == AppClaimTypes.KiraciId))
        {
            var isScopeAware = context.ActionDescriptor.EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .Any(a => a.Policy != null && PermissionCatalog.IsScopeAware(a.Policy));

            if (isScopeAware)
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    var dto = await _cache.GetAsync(userId);
                    _provider.Initialize(dto);
                }
            }
        }

        await next();
    }
}
