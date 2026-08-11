using KiraTakip.Authorization;
using KiraTakip.Models.Dtos.AuditLog;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.Audit.Module)]
[Route("Admin/AuditLog")]
public class AdminAuditLogController(IAuditService auditService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] TableQuery query,
        string? eventType,
        string? entityType,
        string? userEmail,
        DateTime? startDate,
        DateTime? endDate)
    {
        query.From ??= startDate;
        query.To ??= endDate;

        var input = new QueryInput(
            eventType,
            entityType,
            userEmail,
            query);

        var result = await auditService.QueryAsync(input);

        return View(new AuditLogFilterViewModel
        {
            EventType = eventType,
            EntityType = entityType,
            UserEmail = userEmail,
            Query = query,
            Records = result.Records,
            AvailableEventTypes = result.AvailableEventTypes,
            AvailableEntityTypes = result.AvailableEntityTypes,
            UserNotFoundMessage = result.UserNotFoundMessage
        });
    }
}
