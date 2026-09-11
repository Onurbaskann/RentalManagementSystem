using KiraTakip.Web.Authorization;
using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models.Dtos.Tenant;

namespace KiraTakip.Web.Controllers;

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
