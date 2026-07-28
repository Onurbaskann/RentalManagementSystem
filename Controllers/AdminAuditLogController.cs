using KiraTakip.Authorization;
using KiraTakip.Models.Dtos.AuditLog;
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
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterViewModel filter)
    {
        filter.Page = Math.Max(1, filter.Page);

        var input = new QueryInput(
            filter.EventType,
            filter.EntityType,
            filter.StartDate,
            filter.EndDate,
            filter.UserEmail,
            filter.Page,
            AuditLogFilterViewModel.PageSize);

        var result = await auditService.QueryAsync(input);

        filter.TotalCount = result.TotalCount;
        filter.AvailableEventTypes = result.AvailableEventTypes;
        filter.AvailableEntityTypes = result.AvailableEntityTypes;
        filter.UserNotFoundMessage = result.UserNotFoundMessage;
        filter.Records = result.Rows.Select(r => new AuditLogRowViewModel
        {
            Id = r.Id,
            EventType = r.EventType,
            EntityType = r.EntityType,
            EntityId = r.EntityId,
            UserFullName = r.UserFullName,
            IpAddress = r.IpAddress,
            Details = r.Details,
            CreatedAt = r.CreatedAt
        }).ToList();

        return View(filter);
    }
}
