using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KiraTakip.Services.Interfaces.Identity;

namespace KiraTakip.Web.Authorization;

public class RequireKiraciIdAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserContext>();
        if (!currentUser.TenantId.HasValue)
        {
            context.Result = new ForbidResult();
            return;
        }
        base.OnActionExecuting(context);
    }
}
