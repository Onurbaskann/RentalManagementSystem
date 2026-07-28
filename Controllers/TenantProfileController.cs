using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Route("Tenant/Profile")]
public class TenantProfileController(
    ITenantService tenantService,
    ICurrentUserContext currentUserContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var tenant = await tenantService.GetProfileAsync(new GetTenantProfileInput(tenantId));

        return View(tenant);
    }
}
