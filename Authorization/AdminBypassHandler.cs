using Microsoft.AspNetCore.Authorization;

namespace KiraTakip.Authorization;

public class AdminBypassHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.User.IsInRole("Admin"))
        {
            foreach (var req in context.PendingRequirements.ToList())
                context.Succeed(req);
        }
        return Task.CompletedTask;
    }
}
