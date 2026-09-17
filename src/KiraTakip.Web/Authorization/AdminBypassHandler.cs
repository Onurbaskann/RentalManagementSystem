using Microsoft.AspNetCore.Authorization;

namespace KiraTakip.Web.Authorization;

public class AdminBypassHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (!PermissionEvaluator.IsSuperAdmin(context.User))
            return Task.CompletedTask;

        foreach (var req in context.PendingRequirements.ToList())
        {
            if (req is PermissionRequirement permReq)
            {
                if (PermissionEvaluator.IsKnownPermission(permReq.Permission))
                {
                    context.Succeed(req);
                }
            }
            else
            {
                context.Succeed(req);
            }
        }

        return Task.CompletedTask;
    }
}
